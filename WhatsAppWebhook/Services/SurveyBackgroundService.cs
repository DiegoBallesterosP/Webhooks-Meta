using WhatsAppWebhook.Services.HistoryLogs;
using WhatsAppWebhook.Services.SendMessage;

namespace WhatsAppWebhook.Services
{
    public class SurveyBackgroundService : BackgroundService
    {
        private readonly ILogger<SurveyBackgroundService> _logger;
        private readonly WhatsAppSenderService _sender;
        private readonly CosmosDbService _cosmosDbService;

        public SurveyBackgroundService(
            ILogger<SurveyBackgroundService> logger,
            WhatsAppSenderService sender,
            CosmosDbService cosmosDbService)
        {
            _logger = logger;
            _sender = sender;
            _cosmosDbService = cosmosDbService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var customers = await _cosmosDbService.GetRandomCustomersWithoutSurveyAsync(0.2);

                foreach (var to in customers)
                {
                    await _sender.SendSurveyAsync(to);
                }

                await Task.Delay(TimeSpan.FromDays(7), stoppingToken);
            }
        }
    }
}