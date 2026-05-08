namespace ECommerce.Infrastructure.Persistence.Events;

public sealed record PaymentSucceededEvent(Guid PaymentId, Guid OrderId, Guid CustomerId, decimal Amount, DateTime OccurredAt);
