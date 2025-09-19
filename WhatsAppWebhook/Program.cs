using Amazon;
using Amazon.Runtime;
using Amazon.TranscribeStreaming;
using WhatsAppWebhook.Models.ConnectionCloud;
using WhatsAppWebhook.Services;
using WhatsAppWebhook.Services.ConnectionCloud;
using WhatsAppWebhook.Services.ConnectionModel;
using WhatsAppWebhook.Services.HistoryLogs;
using WhatsAppWebhook.Services.SendMessage;

EventLog.RegisterClassMap();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IAmazonTranscribeStreaming>(provider =>
{
    var config = provider.GetService<IConfiguration>();
    var region = config?["AWS:Region"] ?? "us-east-1";
    var accessKey = config?["AWS:AccessKeyId"];
    var secretKey = config?["AWS:SecretAccessKey"];
    var creds = new BasicAWSCredentials(accessKey, secretKey);
    return new AmazonTranscribeStreamingClient(creds, RegionEndpoint.GetBySystemName(region));
});

builder.Services.AddSingleton<CosmosDbService>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new CosmosDbService(configuration);
});


builder.Services.AddScoped<LogService>();

// Servicios de WhatsApp y audio
builder.Services.AddHttpClient<AudioService>();
builder.Services.AddHttpClient<WhatsAppSenderService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddHttpClient<CloudApiService>();
builder.Services.AddHttpClient<ConnectionApiModel>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();