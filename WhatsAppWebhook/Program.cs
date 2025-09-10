using WhatsAppWebhook.Services;
using Amazon.TranscribeStreaming;
using Amazon;
using Amazon.Runtime;

var builder = WebApplication.CreateBuilder(args);

// Servicios y configuración
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cliente AWS con credenciales de appsettings
builder.Services.AddSingleton<IAmazonTranscribeStreaming>(provider =>
{
    var config = provider.GetService<IConfiguration>();
    var region = config["AWS:Region"] ?? "us-east-1";
    var accessKey = config["AWS:AccessKeyId"];
    var secretKey = config["AWS:SecretAccessKey"];

    var creds = new BasicAWSCredentials(accessKey, secretKey);
    return new AmazonTranscribeStreamingClient(creds, RegionEndpoint.GetBySystemName(region));
});

builder.Services.AddHttpClient<AudioService>();
builder.Services.AddHttpClient<WhatsAppSenderService>();
builder.Services.AddScoped<MessageService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
