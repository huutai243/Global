namespace ECommerce.Shared.Contracts;

public sealed record InventoryReservedEvent(
    Guid OrderId,
    Guid CustomerId,
    IReadOnlyCollection<InventoryReservedItem> Items,
    DateTime ReservedAtUtc);

public sealed record InventoryReservedItem(
    Guid ProductId,
    int Quantity);