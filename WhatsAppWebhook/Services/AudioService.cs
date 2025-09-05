using System.Net.Http.Headers;
using Amazon.TranscribeStreaming;
using Amazon.TranscribeStreaming.Model;
using System.Text;

namespace WhatsAppWebhook.Services
{
    public class AudioService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;
        private readonly IAmazonTranscribeStreaming _transcribeClient;

        public AudioService(IConfiguration config, HttpClient http, IAmazonTranscribeStreaming transcribeClient)
        {
            _config = config;
            _http = http;
            _transcribeClient = transcribeClient;
        }

        public async Task<byte[]?> GetAudioBytesAsync(string mediaId)
        {
            var baseUrl = _config["WhatsApp:ApiBaseUrl"];
            var token = _config["WhatsApp:AccessToken"];
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var metaUrl = $"{baseUrl}{mediaId}";
                var metaResponse = await _http.GetFromJsonAsync<Dictionary<string, object>>(metaUrl);

                if (metaResponse?.TryGetValue("url", out var urlObj) != true || string.IsNullOrEmpty(urlObj?.ToString()))
                    return null;

                using var request = new HttpRequestMessage(HttpMethod.Get, urlObj.ToString());
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _http.SendAsync(request);
                return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
            }
            catch (Exception ex)
            {
                LogService.SaveLog("webhook-error", $"Error obteniendo audio {mediaId}: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> TranscribeAsync(byte[] audioBytes)
        {
            var configs = new[]
            {
                new { Encoding = MediaEncoding.OggOpus, SampleRate = 16000 },
                new { Encoding = MediaEncoding.OggOpus, SampleRate = 8000 },
                new { Encoding = MediaEncoding.Pcm, SampleRate = 16000 },
                new { Encoding = MediaEncoding.Pcm, SampleRate = 8000 }
            };

            foreach (var config in configs)
            {
                try
                {
                    using var stream = new MemoryStream(audioBytes);
                    var transcript = await TryTranscribeWithConfig(stream, config.Encoding, config.SampleRate);

                    if (!string.IsNullOrWhiteSpace(transcript))
                    {
                        LogService.SaveLog("webhook-info", $"Transcripción exitosa con {config.Encoding} a {config.SampleRate}Hz");
                        return transcript;
                    }
                }
                catch (Exception ex)
                {
                    LogService.SaveLog("webhook-debug", $"Falló transcripción con {config.Encoding}@{config.SampleRate}Hz: {ex.Message}");
                }
            }

            LogService.SaveLog("webhook-warning", "No se pudo transcribir con ninguna configuración");
            return null;
        }

        private async Task<string?> TryTranscribeWithConfig(Stream audioStream, MediaEncoding encoding, int sampleRate)
        {
            var request = new StartStreamTranscriptionRequest
            {
                LanguageCode = LanguageCode.EsES,
                MediaEncoding = encoding,
                MediaSampleRateHertz = sampleRate,
                AudioStream = new AudioEventStream(audioStream)
            };

            var sb = new StringBuilder();

            using var response = await _transcribeClient.StartStreamTranscriptionAsync(request, CancellationToken.None);

            await foreach (var evt in response.TranscriptResultStream)
            {
                if (evt is TranscriptEvent transcriptEvent)
                {
                    foreach (var result in transcriptEvent.Transcript.Results)
                    {
                        if (result.IsPartial == true || result.Alternatives == null)
                            continue;

                        foreach (var alt in result.Alternatives)
                        {
                            if (!string.IsNullOrWhiteSpace(alt.Transcript))
                                sb.AppendLine(alt.Transcript);
                        }
                    }
                }
            }

            return sb.Length > 0 ? sb.ToString().Trim() : null;
        }

        private class AudioEventStream : IAsyncEnumerable<AudioEvent>
        {
            private readonly Stream _source;

            public AudioEventStream(Stream source) => _source = source;

            public async IAsyncEnumerator<AudioEvent> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                var buffer = new byte[3200];
                int bytesRead;

                while ((bytesRead = await _source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    yield return new AudioEvent
                    {
                        AudioChunk = new MemoryStream(buffer, 0, bytesRead, writable: false)
                    };

                    await Task.Delay(100, cancellationToken);
                }
            }
        }
    }
}