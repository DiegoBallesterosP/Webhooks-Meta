using System;
using TimeZoneConverter;
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
            var colombiaTimeZone = TZConvert.GetTimeZoneInfo("SA Pacific Standard Time");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, colombiaTimeZone);
                var nextRun = now.Date.AddHours(8); 

                if (now > nextRun)
                    nextRun = nextRun.AddDays(7);

                var delay = nextRun - now;

                await Task.Delay(delay, stoppingToken);

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