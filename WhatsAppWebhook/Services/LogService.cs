namespace WhatsAppWebhook.Services;

public static class LogService
{
    public static void LogVerification(string hubChallenge)
    {
        var logMessage = $"VERIFICACIÓN |Challenge: {hubChallenge}";
        SaveLog("webhook-verify", logMessage);
    }

    public static void LogWebhook(string rawBody)
    {
        SaveLog("webhook-received", rawBody);
    }

    private static void SaveLog(string logType, string content)
    {
        try
        {
            var logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            var logFile = Path.Combine(logDir, $"{logType}-{DateTime.Now:yyyyMMdd}.txt");
            var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {content}";

            File.AppendAllText(logFile, logMessage + Environment.NewLine);
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
            var logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            var errorFile = Path.Combine(logDir, $"errors-{DateTime.Now:yyyyMMdd}.txt");
            var errorLog = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | ERROR | {errorMessage}";

            File.AppendAllText(errorFile, errorLog + Environment.NewLine);
            Console.WriteLine(errorLog);
        }
        catch
        {
            Console.WriteLine($"Error crítico: No se pudo guardar el log de error: {errorMessage}");
        }
    }
}