namespace ECommerce.Shared.Contracts.Payment;

public sealed record PaymentSucceededEvent(
    Guid OrderId,
    Guid CustomerId,
    Guid PaymentTransactionId,
    string Provider,
    string ProviderTransactionId,
    decimal Amount,
    string Currency,
    DateTime OccurredAtUtc);