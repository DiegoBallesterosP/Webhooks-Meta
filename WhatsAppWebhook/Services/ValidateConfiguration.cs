using WhatsAppWebhook.Repositories;
using WhatsAppWebhook.Services.ConnectionCloud;

namespace WhatsAppWebhook.Services
{
    public class ValidateConfiguration
    {
        // NO VA A EXISITR ESTE ARCHIVO ACA EN ESTE PROYECTO ELIMINAR
        private readonly IConfigurationWhatsAppNumberRepository _repository;
        private readonly CloudApiService _cloudApiService;
        public ValidateConfiguration(IConfigurationWhatsAppNumberRepository repository, CloudApiService cloudApiService)
        {
            _repository = repository;
            _cloudApiService = cloudApiService;
        }
        public async Task<bool> ExistValidConfiguration(string whatsAppNumber)
        {
            var config = await _repository.GetByNumeroAsync(whatsAppNumber);
            if (config is not null) return true;

            var resultExternalSearch = await _cloudApiService.ValidateExitsConfigurationByNumber(whatsAppNumber);
            if(!resultExternalSearch) return false;

            await _repository.UpsertAsync(new Models.Cache.ConfigurationWhatsAppNumber(whatsAppNumber));

            return true;
        }
    }
}
