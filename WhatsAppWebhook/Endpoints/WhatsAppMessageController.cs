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

        public WhatsAppMessageController(WhatsAppSenderService sender)
        {
            _sender = sender;
        }

        [HttpPost("message")]
        public async Task<IActionResult> SendText([FromBody] SendTextRequest request)
        {
            var result = await _sender.SendTextAsync(request.To, request.Message);
            return Ok(new { Success = true, Response = result });
        }
    }
}