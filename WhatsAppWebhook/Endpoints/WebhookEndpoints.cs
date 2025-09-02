using Microsoft.AspNetCore.Mvc;
using WhatsAppWebhook.Services;

namespace WhatsAppWebhook.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this WebApplication app)
    {
        app.MapGet("/subscribe", (
            [FromQuery(Name = "hub.mode")] string hubMode,
            [FromQuery(Name = "hub.challenge")] string hubChallenge,
            [FromQuery(Name = "hub.verify_token")] string verifyToken,
            IConfiguration config,
            ILogger<Program> logger) =>
        {
            var expectedToken = config["Webhook:VerificationToken"];

            logger.LogInformation("Verifying webhook: mode={hubMode}, challenge={hubChallenge}, token={verifyToken}", hubMode, hubChallenge, verifyToken);

            if (hubMode == "subscribe" && verifyToken == expectedToken)
                return Results.Text(hubChallenge, "text/plain");

            return Results.Unauthorized();
        })
        .AllowAnonymous()
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