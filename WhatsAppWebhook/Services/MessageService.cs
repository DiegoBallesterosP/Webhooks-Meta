using System.Text.Json;
using WhatsAppWebhook.Models.ConnectionCloud;
using WhatsAppWebhook.Models.Enum;
using WhatsAppWebhook.Services.ConnectionModel;
using WhatsAppWebhook.Services.HistoryLogs;
using WhatsAppWebhook.Services.SendMessage;


namespace WhatsAppWebhook.Services
{
    public class MessageService
    {
        private readonly WhatsAppSenderService _sender;
        private readonly ConnectionApiModel _connectionApiModel;
        private readonly CosmosDbService _cosmosDbService;



        public MessageService(
            WhatsAppSenderService sender,
            ConnectionApiModel connectionApiModel,
            CosmosDbService cosmosDbService
            )
        {
            _sender = sender;
            _connectionApiModel = connectionApiModel;
            _cosmosDbService = cosmosDbService;
        }

        public async Task ProcessWebhookAsync(string rawBody)
        {
            LogService.SaveLog("webhook-message", $"{rawBody}");
            var messages = WebhookParser.Parse(rawBody);
            foreach (MessageLog msg in messages)
            {


                if (msg.Sender == _sender.SenderId)
                {
                    LogService.SaveLog("webhook-skip", $"Ignored self message from {msg.Sender}");
                    continue;
                }

                if (msg.Type == "interactive")
                {
                    await SaveSurveyAsync(msg);
                    continue;
                }

                if (msg.Type != "text" && msg.Type != "audio")
                {
                    await SendUnsupportedTypeResponse(msg);
                    continue;
                }

                _connectionApiModel.SendChatAsync(msg);
            }
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

        public async Task SaveSurveyAsync(MessageLog msg)
        {
            var ratingsMap = new Dictionary<string, RatingEnum>(StringComparer.OrdinalIgnoreCase)
            {
                { "excelente", RatingEnum.Excelente },
                { "buena", RatingEnum.Buena },
                { "ni buena ni mala", RatingEnum.NiBuenaNiMala },
                { "mala", RatingEnum.Mala },
                { "muy mala", RatingEnum.MuyMala }
            };

            int rating = 0;
            string comments = msg.Content;


            var parts = msg.Content.Split('|', 2);
            string rawRating = parts[0];
            if (parts.Length > 1) comments = parts[1];

            foreach (var kvp in ratingsMap)
            {
                if (rawRating.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    rating = (int)kvp.Value;
                    break;
                }
            }

            await _cosmosDbService.AddSurveyAsync(new SurveyWhLog
            {
                Id = Guid.NewGuid().ToString(),
                OriginNumber = msg.Sender,
                Rating = rating.ToString(),
                Comments = comments?? "Sin comentarios",
                EventDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            });
        }




    }
}