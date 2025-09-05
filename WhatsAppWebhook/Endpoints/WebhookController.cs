using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsAppWebhook.Services;

namespace WhatsAppWebhook.Endpoints
{
    public class WebhookController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<WebhookController> _logger;
        private readonly MessageService _messageService;


        public WebhookController(IConfiguration config, ILogger<WebhookController> logger, MessageService messageService)
        {
            _config = config;
            _logger = logger;
            _messageService = messageService;
        }

        [HttpGet("subscribe")]
        [AllowAnonymous]
        public IActionResult VerifyWebhook(
            [FromQuery(Name = "hub.mode")] string hubMode,
            [FromQuery(Name = "hub.challenge")] string hubChallenge,
            [FromQuery(Name = "hub.verify_token")] string verifyToken)
        {
            var expectedToken = _config["Webhook:VerificationToken"];
            _logger.LogInformation(
                "Verifying webhook: mode={hubMode}, challenge={hubChallenge}, token={verifyToken}",
                hubMode, hubChallenge, verifyToken);

            if (hubMode == "subscribe" && verifyToken == expectedToken)
                return Content(hubChallenge, "text/plain");

            return Unauthorized();
        }

        [HttpPost("subscribe")]
        [AllowAnonymous]
        public async Task<IActionResult> ReceiveWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            await _messageService.ProcessWebhookAsync(rawBody);

            return Ok();
        }
    }
}