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

        public async Task<List<EventLog>> GetItemsAsync()
        {
            try
            {
                return await _eventsCollection.Find(_ => true).ToListAsync();
            }
            catch (Exception ex)
            {
                LogService.SaveLog("mongodb-error", $"Error al obtener datos de MongoDB: {ex.Message}");
                return new List<EventLog>();
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

    }
}