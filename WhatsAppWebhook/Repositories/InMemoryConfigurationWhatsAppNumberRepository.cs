using WhatsAppWebhook.Models.Cache;

namespace WhatsAppWebhook.Repositories
{
    public class InMemoryConfigurationWhatsAppNumberRepository : IConfigurationWhatsAppNumberRepository
    {
        private readonly Dictionary<string, ConfigurationWhatsAppNumber> _store = new(StringComparer.OrdinalIgnoreCase);

        public Task<ConfigurationWhatsAppNumber?> GetByNumeroAsync(string numero, CancellationToken ct = default)
            => Task.FromResult(_store.TryGetValue(numero, out var value) ? value : null);

        public Task UpsertAsync(ConfigurationWhatsAppNumber entity, CancellationToken ct = default)
        {
            _store[entity.Number] = entity;
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(string numero, CancellationToken ct = default)
        {
            var removed = _store.Remove(numero);
            return Task.FromResult(removed);
        }
    }
}
