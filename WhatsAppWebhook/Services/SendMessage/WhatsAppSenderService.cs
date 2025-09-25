using System.Net.Http.Headers;
using WhatsAppWebhook.Models.ConnectionCloud;
using WhatsAppWebhook.Services.HistoryLogs;

namespace WhatsAppWebhook.Services.SendMessage
{
    public class WhatsAppSenderService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;
        private readonly CosmosDbService _cosmosDbService;

        public WhatsAppSenderService(IConfiguration config, HttpClient http, CosmosDbService cosmosDbService)
        {
            _config = config;
            _http = http;
            _cosmosDbService = cosmosDbService;
        }

        private string BaseUrl => _config["WhatsApp:ApiBaseUrl"] ?? String.Empty;
        public string SenderId => _config["WhatsApp:SenderId"] ?? String.Empty;
        private string AccessToken => _config["WhatsApp:AccessToken"] ?? String.Empty;

        private void SetAuthHeader()
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        }

        public async Task<string> SendTextAsync(string to, string message)
        {
            SetAuthHeader();
            var url = $"{BaseUrl}{SenderId}/messages";
            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = to,
                type = "text",
                text = new { preview_url = false, body = message }
            };

            var response = await _http.PostAsJsonAsync(url, payload);
            var content = await response.Content.ReadAsStringAsync();

            if (!message.Contains("Espera la respuesta antes de enviar un nuevo mensaje.", StringComparison.OrdinalIgnoreCase))
            {
                _ = Task.Run(() => _cosmosDbService.AddItemAsync(new EventLog
                {
                    EventType = "Answer-Text",
                    Payload = message,
                    OriginNumber = SenderId,
                    DestinationNumber = to,
                    Status = response.IsSuccessStatusCode ? "SENT" : "FAILED",
                    EventDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Role = "assistant"
                }));
            }

            LogService.SaveLog("whatsapp-send-debug", $"To: {to} | Message: {message} | Status: {response.StatusCode} | Response: {content}");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error sending WhatsApp message: {response.StatusCode} - {content}");

            return content;
        }

        public async Task<string> SendTemplateAsync(string to, string otpCode)
        {
            SetAuthHeader();
            var url = $"{BaseUrl}{SenderId}/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                to = to,
                type = "template",
                template = new
                {
                    name = "mensaje_prueba_cloud",
                    language = new { code = "es_CO" },
                    components = new object[]
                    {
                        new
                        {
                            type = "body",
                            parameters = new object[]
                            {
                                new { type = "text", text = otpCode }
                            }
                        },
                            new
                            {
                                type = "button",
                                sub_type = "url",
                                index = "0",
                                parameters = new object[]
                                {
                                    new { type = "text", text = otpCode }
                                }
                            }
                    }
                }
            };

            var response = await _http.PostAsJsonAsync(url, payload);
            var content = await response.Content.ReadAsStringAsync();

            // Solo logueo local
            LogService.SaveLog("whatsapp-send-debug",
                $"To: {to} | OTP: {otpCode} | Status: {response.StatusCode} | Response: {content}");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error sending WhatsApp template: {response.StatusCode} - {content}");

            return content;
        }




    }
}