using WhatsAppWebhook.Endpoints;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapWebhookEndpoints();
app.Run();