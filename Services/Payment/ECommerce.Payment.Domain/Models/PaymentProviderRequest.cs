namespace ECommerce.Payment.Domain.Models;

public sealed record PaymentProviderRequest(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    string IdempotencyKey,
    string PaymentMethod);