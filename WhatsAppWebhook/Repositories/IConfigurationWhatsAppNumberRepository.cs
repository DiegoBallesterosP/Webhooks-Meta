using WhatsAppWebhook.Models.Cache;

namespace WhatsAppWebhook.Repositories
{
    public interface IConfigurationWhatsAppNumberRepository
    {
        Task<ConfigurationWhatsAppNumber?> GetByNumeroAsync(string numero, CancellationToken ct = default);
        Task UpsertAsync(ConfigurationWhatsAppNumber entity, CancellationToken ct = default);
        Task<bool> DeleteAsync(string numero, CancellationToken ct = default);
    }
}
