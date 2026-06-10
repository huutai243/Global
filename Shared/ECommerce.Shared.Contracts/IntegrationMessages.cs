namespace ECommerce.Shared.Contracts;

public sealed record ReserveInventoryCommand(
    Guid OrderId,
    Guid CustomerId,
    IReadOnlyCollection<InventoryReservationItem> Items,
    DateTime RequestedAtUtc);

public sealed record InventoryReservationItem(
    Guid ProductId,
    string ProductName,
    int Quantity);

public sealed record PaymentSucceededEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    DateTime PaidAtUtc);

public sealed record PaymentFailedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Reason,
    DateTime FailedAtUtc);