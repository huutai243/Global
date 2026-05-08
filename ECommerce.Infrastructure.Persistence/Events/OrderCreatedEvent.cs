namespace ECommerce.Infrastructure.Persistence.Events;

public sealed record OrderCreatedEvent(Guid OrderId, Guid CustomerId, decimal TotalAmount, DateTime OccurredAt);
