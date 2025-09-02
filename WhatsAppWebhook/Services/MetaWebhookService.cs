using WhatsAppWebhook.Models;

namespace WhatsAppWebhook.Services
{
    public class MetaWebhookService
    {
        public MetaWebhookService(ILogger<MetaWebhookService> logger)
        {
        }

        public async Task ProcessAsync(RequestWebHook request)
        {
            try
            {
                Console.WriteLine($"Procesando webhook - Sender: {request.SenderId}, Message: {request.Message}");
                await Task.Delay(100);

                Console.WriteLine("Webhook procesado exitosamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al procesar webhook: {ex.Message}");
                throw;
            }
        }
    }
}