namespace ECommerce.Shared.Contracts.Ordering;

public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    IReadOnlyCollection<OrderCreatedItem> Items,
    DateTime CreatedAtUtc);

public sealed record OrderCreatedItem(Guid ProductId, int Quantity, decimal UnitPrice);