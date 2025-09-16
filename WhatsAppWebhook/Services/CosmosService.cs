using MongoDB.Driver;
using WhatsAppWebhook.Models;

namespace WhatsAppWebhook.Services
{
    public class CosmosDbService
    {
        private readonly IMongoCollection<EventLog> _eventsCollection;

        public CosmosDbService(IConfiguration configuration)
        {
            // Mover la inicialización aquí
            EventLog.RegisterClassMap();

            var connectionString = configuration["CosmosDb:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("La 'ConnectionString' de CosmosDb no puede ser nula. Revisa tu appsettings.json.");
            }

            var client = new MongoClient(connectionString);
            
            var database = client.GetDatabase(configuration["CosmosDb:DatabaseName"]);
            _eventsCollection = database.GetCollection<EventLog>(configuration["CosmosDb:ContainerName"]);
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
    }
}