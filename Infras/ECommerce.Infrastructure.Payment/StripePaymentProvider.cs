using ECommerce.Payment.Domain.Interfaces;
using ECommerce.Payment.Domain.Models;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace ECommerce.Infrastructure.Payment;

public sealed class StripePaymentProvider(
    IOptions<StripeOptions> options)
    : IPaymentGateway
{
    private readonly StripeOptions _options = options.Value;

    public string ProviderName => "Stripe";

    public async Task<PaymentProviderResult> CreatePaymentSessionAsync(
        PaymentProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateOptions();
        ValidateRequest(request);

        var sessionOptions = CreateSessionOptions(request);
        var requestOptions = CreateRequestOptions(request);

        var service = new SessionService();

        var session = await service.CreateAsync(
            sessionOptions,
            requestOptions,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Id))
        {
            throw new InvalidOperationException("Stripe returned an invalid checkout session id.");
        }

        if (string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException("Stripe returned an invalid checkout session url.");
        }

        return new PaymentProviderResult(
            session.Id,
            session.Url);
    }

    private SessionCreateOptions CreateSessionOptions(PaymentProviderRequest request)
    {
        return new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = BuildReturnUrl(_options.SuccessUrl, request.OrderId),
            CancelUrl = BuildReturnUrl(_options.CancelUrl, request.OrderId),
            ClientReferenceId = request.OrderId.ToString(),
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = request.OrderId.ToString(),
                ["customerId"] = request.CustomerId.ToString(),
                ["idempotencyKey"] = request.IdempotencyKey,
                ["paymentMethod"] = request.PaymentMethod
            },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency.ToLowerInvariant(),
                        UnitAmount = ToStripeAmount(request.Amount, request.Currency),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Order {request.OrderId}"
                        }
                    }
                }
            ]
        };
    }

    private RequestOptions CreateRequestOptions(PaymentProviderRequest request)
    {
        return new RequestOptions
        {
            ApiKey = _options.SecretKey,
            IdempotencyKey = request.IdempotencyKey
        };
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new InvalidOperationException("Stripe SecretKey is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.SuccessUrl))
        {
            throw new InvalidOperationException("Stripe SuccessUrl is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.CancelUrl))
        {
            throw new InvalidOperationException("Stripe CancelUrl is required.");
        }
    }

    private static void ValidateRequest(PaymentProviderRequest request)
    {
        if (request.OrderId == Guid.Empty)
        {
            throw new InvalidOperationException("Payment request OrderId is required.");
        }

        if (request.CustomerId == Guid.Empty)
        {
            throw new InvalidOperationException("Payment request CustomerId is required.");
        }

        if (request.Amount <= 0)
        {
            throw new InvalidOperationException("Payment request Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Currency))
        {
            throw new InvalidOperationException("Payment request Currency is required.");
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new InvalidOperationException("Payment request IdempotencyKey is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
        {
            throw new InvalidOperationException("Payment request PaymentMethod is required.");
        }
    }

    private static string BuildReturnUrl(string url, Guid orderId)
    {
        var separator = url.Contains('?') ? "&" : "?";

        return $"{url}{separator}orderId={orderId}";
    }

    private static long ToStripeAmount(decimal amount, string currency)
    {
        return IsZeroDecimalCurrency(currency)
            ? decimal.ToInt64(decimal.Round(amount, 0, MidpointRounding.AwayFromZero))
            : decimal.ToInt64(decimal.Round(amount * 100, 0, MidpointRounding.AwayFromZero));
    }

    private static bool IsZeroDecimalCurrency(string currency)
    {
        return string.Equals(currency, "VND", StringComparison.OrdinalIgnoreCase)
            || string.Equals(currency, "JPY", StringComparison.OrdinalIgnoreCase)
            || string.Equals(currency, "KRW", StringComparison.OrdinalIgnoreCase);
    }
}