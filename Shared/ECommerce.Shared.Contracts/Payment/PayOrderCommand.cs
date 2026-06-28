namespace ECommerce.Shared.Contracts.Payment;

public sealed record PayOrderCommand(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    string IdempotencyKey,
    DateTime OccurredAtUtc);