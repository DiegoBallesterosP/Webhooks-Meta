using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace WhatsAppWebhook.Models.ConnectionCloud
{
    public class EventLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string MessageId { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        [BsonElement("numero_origen")]
        public string? OriginNumber { get; set; } = string.Empty;
        public string? DestinationNumber { get; set; } = string.Empty;
        public string Status { get; set; } = "PROCESSED";

        public string EventDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        public string Role { get; set; } = "user";

        public static void RegisterClassMap()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(EventLog)))
            {
                BsonClassMap.RegisterClassMap<EventLog>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                });
            }
        }
    }
}