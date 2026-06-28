namespace ECommerce.Shared.Contracts.Inventory;

public sealed record InventoryReservedEvent(
    Guid OrderId,
    Guid CustomerId,
    IReadOnlyCollection<InventoryReservedItem> Items,
    DateTime ReservedAtUtc);

public sealed record InventoryReservedItem(
    Guid ProductId,
    int Quantity);