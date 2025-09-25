using WhatsAppWebhook.Models.ConnectionCloud;
using WhatsAppWebhook.Models.ConnectionModelAI;
using WhatsAppWebhook.Services.ConnectionCloud;
using WhatsAppWebhook.Services.ConnectionModel;
using WhatsAppWebhook.Services.Constants;
using WhatsAppWebhook.Services.HistoryLogs;
using WhatsAppWebhook.Services.SendMessage;


namespace WhatsAppWebhook.Services
{
    public class MessageService
    {
        private readonly AudioService _audioService;
        private readonly WhatsAppSenderService _sender;
        private readonly CosmosDbService _cosmosDbService;
        private readonly ConnectionApiModel _connectionApiModel;

        private readonly CloudApiService _cloudApiService;
        private readonly ValidateConfiguration _validateConfiguration;

        public MessageService(
            AudioService audioService,
            WhatsAppSenderService sender,
            CosmosDbService cosmosDbService,
            CloudApiService cloudApiService,
            ConnectionApiModel connectionApiModel,
            ValidateConfiguration validateConfiguration
            )
        {
            _audioService = audioService;
            _sender = sender;
            _cosmosDbService = cosmosDbService;
            _cloudApiService = cloudApiService;
            _connectionApiModel = connectionApiModel;
            _validateConfiguration = validateConfiguration;
        }

        public async Task ProcessWebhookAsync(string rawBody)
        {
            var messages = WebhookParser.Parse(rawBody);
            foreach (var msg in messages)
            {
                LogService.SaveLog("webhook-message", $"{msg.TimestampUtc:O} | TEXT | {msg.SenderName} ({msg.Sender}) | {msg.Content}");

                if (msg.Sender == _sender.SenderId)
                {
                    LogService.SaveLog("webhook-skip", $"Ignored self message from {msg.Sender}");
                    continue;
                }

                if (msg.Type != "text" && msg.Type != "audio")
                {
                    await SendUnsupportedTypeResponse(msg);
                    continue;
                }

                var resultValidation = await _validateConfiguration.ExistValidConfiguration(msg.Sender);
                if (!resultValidation)
                {
                    await _sender.SendTextAsync(msg.Sender, MessageSystem.DefaltMessageNotConfiguration);
                    continue;
                }


                // OBTNER LOS DATOS PARA ENVIAR AL MODELO
                RequestChat requestChat = await BuildRequestChatAsync(msg);


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
                        break;
                }
                
                 // AKI IRA LA API DE CHATGPT O SIMILAR QUE RECIBIRA LA INFORMACION SEA PRIMERA VEZ O NO
                await _connectionApiModel.SendChatAsync(requestChat);
            }
        }

        private async Task<RequestChat> BuildRequestChatAsync(MessageLog msg)
        {
            // 👉 Verificar si se debe enviar mensaje de bienvenida
            var shouldGreet = await _cosmosDbService.ShouldGreetAsync(msg.Sender);

            if (shouldGreet)
            {
                // 👉 Consultar API de Java
                var datosTercero = await _cloudApiService.GetClientInfoAsync(msg.Sender);

                return new RequestChat
                {
                    numberUser = datosTercero?.numeroCelularCompleto ?? msg.Sender,
                    nameUser = datosTercero?.nombreTercero,
                    isFirstMessage = true,
                    messages = new List<Message>()
                };
            }
            else
            {
                // 👉 Consultar historial (últimos 11)
                var historial = await _cosmosDbService.GetConversationHistoryAsync(msg.Sender);

                var ultimos11 = historial
                    .OrderByDescending(h => DateTime.Parse(h.EventDate))
                    .Take(11)
                    .OrderBy(h => DateTime.Parse(h.EventDate))
                    .ToList();

                var messages = ultimos11.Select(h => new Message
                {
                    type = h.Role,
                    message = h.Payload
                }).ToList();

                messages.Add(new Message
                {
                    type = "user",
                    message = msg.Content
                });

                return new RequestChat
                {
                    numberUser = msg.Sender,
                    nameUser = string.Empty,
                    isFirstMessage = false,
                    messages = messages
                };
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
            }
            catch (Exception ex)
            {
                LogService.SaveLog("webhook-error", $"Error enviando respuesta automática: {ex.Message}");
            }
        }

    }
}