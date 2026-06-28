using ECommerce.Infrastructure.Payment;
using ECommerce.Payment.Application.StripeWebhook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;

namespace ECommerce.Payment.WebApi.Controllers;

[ApiController]
[Route("api/payment/stripe/webhook")]
public sealed class StripeWebhookController(
    StripeWebhookHandler handler,
    IOptions<StripeOptions> options)
    : ControllerBase
{
    private readonly StripeOptions _options = options.Value;

    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        var json = await new StreamReader(HttpContext.Request.Body)
            .ReadToEndAsync(cancellationToken);

        var signatureHeader = Request.Headers["Stripe-Signature"];

        var stripeEvent = EventUtility.ConstructEvent(
            json,
            signatureHeader,
            _options.WebhookSecret);

        await handler.HandleAsync(stripeEvent, cancellationToken);

        return Ok();
    }
}