namespace WhatsAppWebhook.Models
{
    public class MessageLog
    {
        public string PhoneNumberId { get; set; }
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public string MessageId { get; set; }
        public string Sender { get; set; }
        public string SenderName { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string RawPayload { get; set; }
    }
}