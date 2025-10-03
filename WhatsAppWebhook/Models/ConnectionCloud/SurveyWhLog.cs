using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace WhatsAppWebhook.Models.ConnectionCloud
{
    public class SurveyWhLog
    {

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("number")]
        public string? OriginNumber { get; set; } = string.Empty;

        public string Rating { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;

        public string EventDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        public static void RegisterClassMap()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(SurveyWhLog)))
            {
                BsonClassMap.RegisterClassMap<SurveyWhLog>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                });
            }
        }
    }
}
