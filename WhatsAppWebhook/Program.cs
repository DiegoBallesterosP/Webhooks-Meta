using WhatsAppWebhook.Services;
using Amazon.TranscribeStreaming;
using Amazon;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IAmazonTranscribeStreaming>(provider =>
{
    var config = provider.GetService<IConfiguration>();
    var region = config["AWS:Region"] ?? "us-east-1";
    return new AmazonTranscribeStreamingClient(RegionEndpoint.GetBySystemName(region));
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