namespace WhatsAppWebhook.Models.Cache
{
    public class ConfigurationWhatsAppNumber
    {
		public string Number { get; }

        public ConfigurationWhatsAppNumber(string number)
        {
            if (string.IsNullOrWhiteSpace(number))
                throw new ArgumentException("Number cannot be null or empty.", nameof(number));

            Number = number;
        }
    }
}
