namespace ECommerce.Shared.Contracts.Inventory;

public sealed record InventoryReservationFailedEvent(
    Guid OrderId,
    Guid CustomerId,
    IReadOnlyCollection<InventoryReservationFailedItem> FailedItems,
    string Reason,
    DateTime FailedAtUtc);

public sealed record InventoryReservationFailedItem(
    Guid ProductId,
    string ProductName,
    int RequestedQuantity,
    int AvailableQuantity);