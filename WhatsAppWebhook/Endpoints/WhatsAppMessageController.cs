using Microsoft.AspNetCore.Mvc;
using System.Formats.Asn1;
using WhatsAppWebhook.Models.SendMessage;
using WhatsAppWebhook.Services.SendMessage;

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

        [HttpPost("sendOtp")]
        public async Task<IActionResult> SendCodeOtp([FromBody] SendTextRequest request)
        {
            var result = await _sender.SendTemplateAsync(request.To, request.Message);
            return Ok(new { Success = true, Response = result });
        }

        [HttpPost("sendSurvey")]
        public async Task<IActionResult> SendSurvey([FromBody] SendTextRequest request)
        {
            var result = await _sender.SendSurveyAsync(request.To);
            return Ok(new { Success = true, Response = result });
        }
    }
}