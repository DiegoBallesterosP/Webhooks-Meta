using System.Net.Http.Headers;

namespace WhatsAppWebhook.Services
{
    public class WhatsAppSenderService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public WhatsAppSenderService(IConfiguration config, HttpClient http)
        {
            _config = config;
            _http = http;
        }

        private string BaseUrl => _config["WhatsApp:ApiBaseUrl"];
        private string AccessToken => _config["WhatsApp:AccessToken"];

        private void SetAuthHeader()
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        }

        public async Task<string> RegisterNumberAsync(int phoneNumber, int pinNumber)
        {
            SetAuthHeader();
            var url = BaseUrl + $"{phoneNumber}/register";
            var body = new
            {
                messaging_product = "whatsapp",
                pin = pinNumber
            };

            var response = await _http.PostAsJsonAsync(url, body);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error al registrar número: {result}");

            return result;
        }

        public async Task<string> SendTextAsync(string to, string message)
        {
            SetAuthHeader();

            var url = BaseUrl + "105221435626048/messages";
            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = to,
                type = "text",
                text = new
                {
                    preview_url = false,
                    body = message
                }
            };

            var response = await _http.PostAsJsonAsync(url, payload);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                LogService.SaveLog("whatsapp-send-success", $"To: {to} | Message: {message} | Status: {response.StatusCode} | Response: {content}");
                Console.WriteLine($"Mensaje enviado exitosamente: {content}");
            }
            else
            {
                LogService.SaveLog("whatsapp-send-error", $"To: {to} | Message: {message} | Status: {response.StatusCode} | Error: {content}");
                Console.WriteLine($"Error al enviar mensaje: {content}");
                throw new HttpRequestException($"Error sending WhatsApp message: {response.StatusCode} - {content}");
            }

            return content;
        }
    }
}