namespace WhatsAppWebhook.Models
{
    public class RequestWebHook
    {
        public string SenderId { get; set; }
        public string RecipientId { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        
        public string Object { get; set; }
        public List<Entry> Entry { get; set; }
    }

    public class Entry
    {
        public string Id { get; set; }
        public List<Change> Changes { get; set; }
    }

    public class Change
    {
        public string Field { get; set; }
        public object Value { get; set; }
    }
}