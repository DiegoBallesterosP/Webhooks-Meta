using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WhatsAppWebhook.Services
{
    public class AudioService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public AudioService(IConfiguration config, HttpClient http)
        {
            _config = config;
            _http = http;
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

        public async Task<string> TranscribeStreamAsync(byte[] audioBytes)
        {
            var region = _config["AWS:Region"] ?? "us-east-1";
            var language = "es-ES";
            var sampleRate = 16000;

            var accessKey = _config["AWS:AccessKeyId"];
            var secretKey = _config["AWS:SecretAccessKey"];
            var service = "transcribe";
            var host = $"transcribestreaming.{region}.amazonaws.com:8443";

            var url = $"https://{host}/stream-transcription";

            var client = new HttpClient { DefaultRequestVersion = new Version(2, 0) };
            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Add("X-Amz-Target", "com.amazonaws.transcribe.Transcribe.StartStreamTranscription");
            request.Headers.Add("X-Amz-Content-Sha256", "STREAMING-AWS4-HMAC-SHA256-EVENTS");
            request.Headers.Add("X-Amz-Date", DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ"));
            request.Headers.Add("x-amzn-transcribe-language-code", language);
            request.Headers.Add("x-amzn-transcribe-sample-rate", sampleRate.ToString());
            request.Headers.Add("x-amzn-transcribe-media-encoding", "pcm");

            request.Headers.Authorization = new AuthenticationHeaderValue("AWS4-HMAC-SHA256", "Credential=..., SignedHeaders=..., Signature=...");

            request.Content = new StreamContent(new MemoryStream(audioBytes));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.amazon.eventstream");

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var transcriptBuilder = new StringBuilder();

            using var responseStream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(responseStream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.Contains("TranscriptEvent"))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        var results = doc.RootElement.GetProperty("Transcript").GetProperty("Results");

                        foreach (var result in results.EnumerateArray())
                        {
                            if (!result.GetProperty("IsPartial").GetBoolean())
                            {
                                foreach (var alt in result.GetProperty("Alternatives").EnumerateArray())
                                {
                                    var text = alt.GetProperty("Transcript").GetString();
                                    if (!string.IsNullOrWhiteSpace(text))
                                        transcriptBuilder.AppendLine(text);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            var transcript = transcriptBuilder.ToString().Trim();
            LogService.SaveLog("transcribe-audio", transcript);
            return transcript;
        }
    }
}
