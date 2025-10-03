namespace WhatsAppWebhook.Models.SendMessage
{
    public class SendTextRequest
    {
        public string To { get; set; }
        public string? Message { get; set; }
    }
}