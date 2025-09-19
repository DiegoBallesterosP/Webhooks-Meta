namespace WhatsAppWebhook.Models.ConnectionModelAI
{
    public class RequestChat
    {
        public string? numberUser { get; set; }
        public bool? isFirstMessage { get; set; }
        public string? nameUser { get; set; }
        public List<Message>? messages { get; set; }

    }
}