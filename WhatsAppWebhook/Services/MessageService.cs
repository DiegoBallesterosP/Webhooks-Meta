using Newtonsoft.Json;
using System.Text.Json.Serialization;
using WhatsAppWebhook.Models.ConnectionCloud;
using WhatsAppWebhook.Services.ConnectionModel;
using WhatsAppWebhook.Services.HistoryLogs;
using WhatsAppWebhook.Services.SendMessage;


namespace WhatsAppWebhook.Services
{
    public class MessageService
    {
        private readonly AudioService _audioService;
        private readonly WhatsAppSenderService _sender;

        private readonly ConnectionApiModel _connectionApiModel;



        public MessageService(
            AudioService audioService,
            WhatsAppSenderService sender,
            ConnectionApiModel connectionApiModel
            )
        {
            _audioService = audioService;
            _sender = sender;
            _connectionApiModel = connectionApiModel;
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

    }
}