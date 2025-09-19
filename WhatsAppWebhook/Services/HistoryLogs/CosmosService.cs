using MongoDB.Driver;
using WhatsAppWebhook.Models.ConnectionCloud;

namespace WhatsAppWebhook.Services.HistoryLogs
{
    public class CosmosDbService
    {
        private readonly IMongoCollection<EventLog> _eventsCollection;

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

        public async Task<bool> ShouldGreetAsync(string originNumber)
        {
            var filter = Builders<EventLog>.Filter.Eq(e => e.OriginNumber, originNumber);
            var sort = Builders<EventLog>.Sort.Descending(e => e.CreatedAt);

            var lastLog = await _eventsCollection.Find(filter).Sort(sort).FirstOrDefaultAsync();

            if (lastLog == null)
                return true;

            if (DateTime.TryParse(lastLog.CreatedAt, out var lastDate))
            {
                return (DateTime.UtcNow - lastDate).TotalDays > 7;
            }

            return true;
        }

        public async Task<List<EventLog>> GetConversationHistoryAsync(string phoneNumber)
        {
            try
            {
                var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

                var filter = Builders<EventLog>.Filter.And(
                    Builders<EventLog>.Filter.Eq(e => e.OriginNumber, phoneNumber),
                    Builders<EventLog>.Filter.Gte(e => e.EventDate, sevenDaysAgo.ToString("yyyy-MM-ddTHH:mm:ssZ"))
                );

                var sort = Builders<EventLog>.Sort.Descending(e => e.EventDate);

                var results = await _eventsCollection
                    .Find(filter)
                    .Sort(sort)
                    .Limit(11)
                    .ToListAsync();

                // devolverlos en orden cronológico (antiguo → nuevo)
                return results
                    .OrderBy(e => DateTime.Parse(e.EventDate))
                    .ToList();
            }
            catch (Exception ex)
            {
                LogService.SaveLog("mongodb-error", $"Error consultando historial en Cosmos: {ex.Message}");
                return new List<EventLog>();
            }
        }

    }
}