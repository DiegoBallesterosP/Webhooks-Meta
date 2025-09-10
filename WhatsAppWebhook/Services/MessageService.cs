using WhatsAppWebhook.Models;

namespace WhatsAppWebhook.Services
{
    public class MessageService
    {
        private readonly AudioService _audioService;
        private readonly WhatsAppSenderService _sender;

        public MessageService(AudioService audioService, WhatsAppSenderService sender)
        {
            _audioService = audioService;
            _sender = sender;
        }

        public async Task ProcessWebhookAsync(string rawBody)
        {
            var messages = WebhookParser.Parse(rawBody);

            foreach (var msg in messages)
            {
                switch (msg.Type)
                {
                    case "text":
                        LogService.SaveLog("webhook-message",
                            $"{msg.TimestampUtc:O} | TEXT | {msg.SenderName} ({msg.Sender}) | {msg.Content}");
                        break;

                    case "audio":
                        await ProcessAudioMessage(msg);
                        break;

                    default:
                        await SendUnsupportedTypeResponse(msg.Sender);
                        break;
                }
            }
        }

        private async Task ProcessAudioMessage(MessageLog msg)
        {
            byte[]? audioBytes = await _audioService.GetAudioBytesAsync(msg.Content);

            if (audioBytes == null)
            {
                LogService.SaveLog("webhook-error", $"No se pudo descargar audio: {msg.Content}");
                return;
            }

            string transcription;
            try
            {
                transcription = await _audioService.TranscribeStreamAsync(audioBytes);
            }
            catch (Exception ex)
            {
                LogService.SaveLog("webhook-error", $"Error transcribiendo audio {msg.Content}: {ex.Message}");
                transcription = null;
            }

            var result = !string.IsNullOrWhiteSpace(transcription)
                ? transcription
                : $"[NO_TRANSCRIBED] media_id={msg.Content}";

            LogService.SaveLog("webhook-message",
                $"{msg.TimestampUtc:O} | AUDIO | {msg.SenderName} ({msg.Sender}) | {result}");

            LogService.SaveLog("webhook-raw", msg.RawPayload);
        }


        private async Task SendUnsupportedTypeResponse(string sender)
        {
            try
            {
                var responseMessage = "Este canal solo acepta mensajes de texto o audio.";
                await _sender.SendTextAsync(sender, responseMessage);
                LogService.SaveLog("auto-response", $"Sent to {sender}: {responseMessage}");
            }
            catch (Exception ex)
            {
                LogService.SaveLog("webhook-error", $"Error enviando respuesta automática: {ex.Message}");
            }
        }
    }
}