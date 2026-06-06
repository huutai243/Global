namespace ECommerce.Shared.Contracts;

public sealed record InventoryReservationItem(Guid ProductId, string ProductName, int Quantity);

public sealed record PaymentOrderItem(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record ReserveInventoryCommand(
    Guid OrderId,
    Guid CustomerId,
    IReadOnlyCollection<InventoryReservationItem> Items,
    DateTime OccurredAt);

public sealed record InventoryReservedEvent(
    Guid OrderId,
    Guid CustomerId,
    DateTime OccurredAt);

public sealed record InventoryReserveFailedEvent(
    Guid OrderId,
    Guid CustomerId,
    string Reason,
    DateTime OccurredAt);

public sealed record CreatePaymentCommand(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    IReadOnlyCollection<PaymentOrderItem> Items,
    DateTime OccurredAt);

public sealed record PaymentCreatedEvent(
    Guid PaymentId,
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    DateTime OccurredAt);

public sealed record PaymentSucceededEvent(
    Guid PaymentId,
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    DateTime OccurredAt);

public sealed record PaymentFailedEvent(
    Guid PaymentId,
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Reason,
    DateTime OccurredAt);

public sealed record ConfirmInventoryReservationCommand(
    Guid OrderId,
    Guid CustomerId,
    DateTime OccurredAt);

public sealed record ReleaseInventoryReservationCommand(
    Guid OrderId,
    Guid CustomerId,
    string Reason,
    DateTime OccurredAt);

public sealed record OrderPaidEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    DateTime OccurredAt);

public sealed record OrderCancelledEvent(
    Guid OrderId,
    Guid CustomerId,
    string Reason,
    DateTime OccurredAt);
