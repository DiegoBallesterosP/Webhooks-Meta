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
        private string SenderId => _config["WhatsApp:SenderId"];
        private string AccessToken => _config["WhatsApp:AccessToken"];

        private void SetAuthHeader()
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        }

        public async Task<string> SendTextAsync(string to, string message)
        {
            SetAuthHeader();

            // URL correcta: Phone Number ID + /messages
            var url = $"{BaseUrl}{SenderId}/messages";

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

            LogService.SaveLog("whatsapp-send-debug", $"To: {to} | Message: {message} | Status: {response.StatusCode} | Response: {content}");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error sending WhatsApp message: {response.StatusCode} - {content}");

            return content;
        }
    }
}