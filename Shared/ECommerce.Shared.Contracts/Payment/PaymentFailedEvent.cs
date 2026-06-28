namespace ECommerce.Shared.Contracts.Payment;

public sealed record PaymentFailedEvent(
    Guid OrderId,
    Guid CustomerId,
    Guid PaymentTransactionId,
    string Provider,
    decimal Amount,
    string Currency,
    string FailureReason,
    DateTime OccurredAtUtc);