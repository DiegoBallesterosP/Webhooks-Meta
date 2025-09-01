using WhatsAppWebhook.Models;
using WhatsAppWebhook.Services;
using Microsoft.AspNetCore.Mvc;

namespace WhatsAppWebhook.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this WebApplication app)
    {
        app.MapGet("/subscribe", 
            [ApiExplorerSettings(GroupName = "v1")]
            ([FromQuery(Name = "hub.mode")] string hubMode,
             [FromQuery(Name = "hub.challenge")] string hubChallenge,
             [FromQuery(Name = "hub.verify_token")] string verifyToken,
             IConfiguration config) =>
        {
            var expectedToken = config["Webhook:VerificationToken"];
            if (!string.IsNullOrEmpty(expectedToken) && verifyToken != expectedToken)            
                return Results.Unauthorized();
            

            if (hubMode != "subscribe")            
                return Results.BadRequest("hub.mode debe ser 'subscribe'");
            

            return Results.Ok(hubChallenge);
        })
        .WithName("VerifyWebhook")
        .WithOpenApi()
        .Produces<string>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status400BadRequest);

        app.MapPost("/subscribe", 
            [ApiExplorerSettings(GroupName = "v1")]
            async ([FromBody] RequestWebHook request) =>
        {
            LogService.LogWebhookRequest(request, "webhookmeta");
            await new MetaWebhookService(null).ProcessAsync(request);
            
            return Results.Ok();
        })
        .WithName("ReceiveWebhook")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK);
    }
}