using MongoDB.Driver;
using WhatsAppWebhook.Models.ConnectionCloud;

namespace WhatsAppWebhook.Services.HistoryLogs
{
    public class CosmosDbService
    {
        private readonly IMongoCollection<EventLog> _eventsCollection;
        private readonly IMongoCollection<SurveyWhLog> _surveyWhatsapp;

        public CosmosDbService(IConfiguration configuration)
        {
            EventLog.RegisterClassMap();

            var connectionString = configuration["CosmosDb:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("La 'ConnectionString' de CosmosDb no puede ser nula. Revisa tu appsettings.json.");
            }

            var client = new MongoClient(connectionString);

            var database = client.GetDatabase(configuration["CosmosDb:DatabaseName"]);
            _eventsCollection = database.GetCollection<EventLog>(configuration["CosmosDb:ContainerName"]);

            _surveyWhatsapp = database.GetCollection<SurveyWhLog>(configuration["CosmosDb:SurveyWhatsappName"]);
        }

        public async Task AddItemAsync(EventLog item)
        {
            try
            {
                await _eventsCollection.InsertOneAsync(item);
            }
            catch (Exception ex)
            {
                LogService.SaveLog("mongodb-error", $"Error al guardar en MongoDB: {ex.Message}");
            }
        }

        public async Task AddSurveyAsync(SurveyWhLog item)
        {
            try
            {
                await _surveyWhatsapp.InsertOneAsync(item);
            }
            catch (Exception ex)
            {
                LogService.SaveLog("mongodb-error", $"Error al guardar encuesta en MongoDB: {ex.Message}");
            }
        }

        public async Task<List<string>> GetRandomCustomersWithoutSurveyAsync(double percentage = 0.2)
        {
            var interactedNumbers = await _eventsCollection
                .Distinct<string>("OriginNumber", FilterDefinition<EventLog>.Empty)
                .ToListAsync();

            if (interactedNumbers.Count == 0)
                return new List<string>();

            var surveyedNumbers = await _surveyWhatsapp
                .Distinct<string>("OriginNumber", FilterDefinition<SurveyWhLog>.Empty)
                .ToListAsync();

            var pendingNumbers = interactedNumbers
                .Except(surveyedNumbers)
                .ToList();

            if (pendingNumbers.Count == 0)
                return new List<string>();

            int sampleSize = (int)Math.Ceiling(pendingNumbers.Count * percentage);

            var random = new Random();
            return pendingNumbers
                .OrderBy(_ => random.Next())
                .Take(sampleSize)
                .ToList();
        }


    }
}