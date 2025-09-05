namespace WhatsAppWebhook.Services
{
    public class LogService
    {
        private static readonly string LogDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        private static readonly string LogFile = Path.Combine(LogDir, "application.log");
        private static readonly string ErrorFile = Path.Combine(LogDir, "errors.log");

        public static void SaveLog(string logType, string content)
        {
            try
            {
                if (!Directory.Exists(LogDir))
                    Directory.CreateDirectory(LogDir);

                var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {logType.ToUpper()} | {content}";
                File.AppendAllText(LogFile, logMessage + Environment.NewLine);
                Console.WriteLine(logMessage);
            }
            catch (Exception ex)
            {
                SaveErrorLog($"Error en {logType}: {ex.Message}");
            }
        }

        private static void SaveErrorLog(string errorMessage)
        {
            try
            {
                if (!Directory.Exists(LogDir))
                    Directory.CreateDirectory(LogDir);

                var errorLog = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | ERROR | {errorMessage}";
                File.AppendAllText(ErrorFile, errorLog + Environment.NewLine);
                Console.WriteLine(errorLog);
            }
            catch
            {
                Console.WriteLine($"Error crítico: No se pudo guardar el log de error: {errorMessage}");
            }
        }
    }
}