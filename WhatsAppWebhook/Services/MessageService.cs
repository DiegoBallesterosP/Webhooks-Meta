using WhatsAppWebhook.Models;


namespace WhatsAppWebhook.Services
{
    public class MessageService
    {
        private readonly AudioService _audioService;
        private readonly WhatsAppSenderService _sender;
        private readonly CosmosDbService _cosmosDbService;

        public MessageService(AudioService audioService, WhatsAppSenderService sender, CosmosDbService cosmosDbService)
        {
            _audioService = audioService;
            _sender = sender;
            _cosmosDbService = cosmosDbService;
        }

        public async Task ProcessWebhookAsync(string rawBody)
        {
            var messages = WebhookParser.Parse(rawBody);
            foreach (var msg in messages)
            {
                if (msg.Sender == _sender.SenderId)
                {
                    LogService.SaveLog("webhook-skip", $"Ignored self message from {msg.Sender}");
                    continue;
                }

                switch (msg.Type)
                {
                    case "text":
                        LogService.SaveLog("webhook-message", $"{msg.TimestampUtc:O} | TEXT | {msg.SenderName} ({msg.Sender}) | {msg.Content}");
                        _ = Task.Run(() => _cosmosDbService.AddItemAsync(new EventLog
                        {
                            EventType = "Webhook-Text",
                            Payload = msg.Content,
                            OriginNumber = msg.Sender,
                            DestinationNumber = msg.PhoneNumberId,
                            Status = "RECEIVED",
                            EventDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            Role = "user"
                        }));
                        break;

                    case "audio":
                        await ProcessAudioMessage(msg);
                        break;

                    default:
                        await SendUnsupportedTypeResponse(msg);
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

            var result = !string.IsNullOrWhiteSpace(transcription) ? transcription : $"[NO_TRANSCRIBED] media_id={msg.Content}";
            LogService.SaveLog("webhook-message", $"{msg.TimestampUtc:O} | AUDIO | {msg.SenderName} ({msg.Sender}) | {result}");

            _ = Task.Run(() => _cosmosDbService.AddItemAsync(new EventLog
            {
                EventType = "Webhook-Audio",
                Payload = result,
                OriginNumber = msg.Sender,
                DestinationNumber = msg.PhoneNumberId,
                Status = string.IsNullOrWhiteSpace(transcription) ? "TRANSCRIBE_FAILED" : "TRANSCRIBED",
                EventDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Role = "user"
            }));
        }


        private async Task SendUnsupportedTypeResponse(MessageLog msg)
        {
            try
            {
                var responseMessage = "Este canal solo acepta mensajes de texto o audio.";
                var response = await _sender.SendTextAsync(msg.Sender, responseMessage);

                LogService.SaveLog("auto-response", $"Sent to {msg.Sender}: {responseMessage}");

                _ = Task.Run(() => _cosmosDbService.AddItemAsync(new EventLog
                {
                    EventType = "API-SendText",
                    Payload = responseMessage,
                    OriginNumber = _sender.SenderId,
                    DestinationNumber = msg.Sender,
                    Status = "SENT",
                    EventDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Role = "assistant"
                }));
            }
            catch (Exception ex)
            {
                LogService.SaveLog("webhook-error", $"Error enviando respuesta automática: {ex.Message}");
            }
        }

    }
}