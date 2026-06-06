namespace ECommerce.Infrastructure.Persistence.Events;

public sealed record PaymentFailedEvent(Guid PaymentId, Guid OrderId, Guid CustomerId, decimal Amount, DateTime OccurredAt);
