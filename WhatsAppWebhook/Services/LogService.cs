using System.Text.Json;

namespace WhatsAppWebhook.Services
{
    public static class LogService
    {
        public static void SetLogText(string content, string logType)
        {
            try
            {
                var timestamp = DateTime.Now;
                var logMessage = $"[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{logType}] {content}";
                var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");

                if (!Directory.Exists(logsDirectory))
                    Directory.CreateDirectory(logsDirectory);

                var logFileName = $"{logType}-{timestamp:yyyyMMdd}.txt";
                var logFilePath = Path.Combine(logsDirectory, logFileName);

                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);

                Console.WriteLine($"Log guardado en: {logFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al escribir log: {ex.Message}");
            }
        }

        public static void LogWebhookRequest(object request, string logType = "webhookmeta")
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                SetLogText(jsonContent, logType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al serializar log: {ex.Message}");
            }
        }
    }
}