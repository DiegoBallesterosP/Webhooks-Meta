using WhatsAppWebhook.Models.ConnectionCloud;
using WhatsAppWebhook.Services.HistoryLogs;

namespace WhatsAppWebhook.Services.ConnectionCloud
{
    public class CloudApiService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public CloudApiService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task<WhatsaapInteraction?> GetClientInfoAsync(string phoneNumber)
        {
            try
            {
                var baseUrl = _config["CloudApi:BaseUrl"];
                var url = $"{baseUrl}/interaccion/{phoneNumber}";
                var response = await _http.GetAsync(url);

                return await response.Content.ReadFromJsonAsync<WhatsaapInteraction>();
            }
            catch (Exception ex)
            {
                LogService.SaveErrorLog($"Error deserializando respuesta: {ex.Message}");
                return null;
            }
        }
    }
}