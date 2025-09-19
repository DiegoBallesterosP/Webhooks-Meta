using WhatsAppWebhook.Models.ConnectionModelAI;
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

        public async Task SendChatAsync(RequestChat requestChat)
        {
            try
            {
                var baseUrl = _config["ModelApi:BaseUrl"];
                var url = $"{baseUrl}/chat";

                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                var response = await _http.PostAsJsonAsync(url, requestChat, cts.Token);

                var result = response.IsSuccessStatusCode
                    ? await response.Content.ReadAsStringAsync()
                    : "Procesando Solicitud";

                await _sender.SendTextAsync(requestChat.numberUser , string.IsNullOrWhiteSpace(result) ? "Procesando Solicitud" : result);

            }
            catch (Exception ex)
            {
                LogService.SaveErrorLog($"Excepción al llamar al modelo: {ex.Message}");
            }
        }
    }
}