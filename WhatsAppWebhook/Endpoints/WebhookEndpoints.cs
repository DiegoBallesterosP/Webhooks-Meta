using WhatsAppWebhook.Services;

namespace WhatsAppWebhook.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this WebApplication app)
    {
        app.MapGet("/subscribe", (HttpRequest request, IConfiguration config) =>
        {
            var hubChallenge = request.Query["hub.challenge"].ToString();
            LogService.LogVerification(hubChallenge);
            return Results.Ok(hubChallenge);
        })
        .WithName("VerifyWebhook");

        app.MapPost("/subscribe", async (HttpRequest request) =>
        {
            using var reader = new StreamReader(request.Body);
            var rawBody = await reader.ReadToEndAsync();
            LogService.LogWebhook(rawBody);
            return Results.Ok();
        })
        .WithName("ReceiveWebhook");
    }
}