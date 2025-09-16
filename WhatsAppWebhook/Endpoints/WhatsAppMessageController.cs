using Microsoft.AspNetCore.Mvc;
using WhatsAppWebhook.Models;
using WhatsAppWebhook.Services;

namespace WhatsAppWebhook.Endpoints
{
    [ApiController]
    [Route("api/whatsapp")]
    public class WhatsAppMessageController : ControllerBase
    {
        private readonly WhatsAppSenderService _sender;
                private readonly CosmosDbService _cosmosDbService;


        public WhatsAppMessageController(WhatsAppSenderService sender,  CosmosDbService cosmosDbService)
        {
            _sender = sender;
            _cosmosDbService = cosmosDbService;
        }

        [HttpPost("message")]
        public async Task<IActionResult> SendText([FromBody] SendTextRequest request)
        {
            var result = await _sender.SendTextAsync(request.To, request.Message);
            return Ok(new { Success = true, Response = result });
        }
    }
}