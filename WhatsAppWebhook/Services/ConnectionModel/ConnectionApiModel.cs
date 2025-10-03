using WhatsAppWebhook.Models.ConnectionCloud;
using WhatsAppWebhook.Services.HistoryLogs;
using WhatsAppWebhook.Services.SendMessage;

namespace WhatsAppWebhook.Services.ConnectionModel
{
    public class ConnectionApiModel
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly WhatsAppSenderService _sender;

        public ConnectionApiModel(HttpClient http, IConfiguration config, WhatsAppSenderService whatsAppSenderService)
        {
            _http = http;
            _config = config;
            _sender = whatsAppSenderService;
        }

        public void SendChatAsync(MessageLog requestChat)
        {
            try
            {
                var baseUrl = _config["ModelApi:BaseUrl"];
                var url = $"{baseUrl}/chat";
                _http.PostAsJsonAsync(url, requestChat);


            }
            catch (Exception ex)
            {
                LogService.SaveErrorLog($"Excepción al llamar al modelo: {ex.Message}");
            }
        }
    }
}