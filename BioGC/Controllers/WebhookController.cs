using BioGC.Data;
using BioGC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using System.IO;
using System.Threading.Tasks;

namespace BioGC.Controllers
{
    [Route("webhook")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly string _webhookSecret;

        public WebhookController(
            IOptions<StripeSettings> stripeSettings,
            ILogger<WebhookController> logger)
        {
            _logger = logger;
            _webhookSecret = stripeSettings.Value.WebhookSecret;
        }

        [HttpPost("stripe")]
        public async Task<IActionResult> HandleStripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json,
                    Request.Headers["Stripe-Signature"], _webhookSecret);



                _logger.LogInformation($"Received Stripe event: {stripeEvent.Type}");

                return Ok();
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Stripe webhook signature error.");
                return BadRequest();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook.");
                return StatusCode(500);
            }
        }
    }
}
