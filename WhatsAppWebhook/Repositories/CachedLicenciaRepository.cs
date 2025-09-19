using Microsoft.Extensions.Caching.Memory;
using WhatsAppWebhook.Models.Cache;

namespace WhatsAppWebhook.Repositories
{
    public class CachedLicenciaRepository : IConfigurationWhatsAppNumberRepository
    {
        private readonly IConfigurationWhatsAppNumberRepository _inner;
        private readonly IMemoryCache _cache;
        private static readonly MemoryCacheEntryOptions _ttl1Day =
            new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1) };
        private const string Prefix = "licencia:";

        public CachedLicenciaRepository(IConfigurationWhatsAppNumberRepository inner, IMemoryCache cache)
        {
            _inner = inner;
            _cache = cache;
        }

        public async Task<ConfigurationWhatsAppNumber?> GetByNumeroAsync(string numero, CancellationToken ct = default)
        {
            var key = Key(numero);
            if (_cache.TryGetValue(key, out ConfigurationWhatsAppNumber? cached))
                return cached;

            var entity = await _inner.GetByNumeroAsync(numero, ct);
            if (entity is not null)
                _cache.Set(key, entity, _ttl1Day); 

            return entity;
        }

        public async Task UpsertAsync(ConfigurationWhatsAppNumber entity, CancellationToken ct = default)
        {
            await _inner.UpsertAsync(entity, ct);
            _cache.Set(Key(entity.Number), entity, _ttl1Day); 
        }

        public async Task<bool> DeleteAsync(string numero, CancellationToken ct = default)
        {
            var removed = await _inner.DeleteAsync(numero, ct);
            _cache.Remove(Key(numero));
            return removed;
        }

        private static string Key(string numero) => $"{Prefix}{numero}";
    }
}
