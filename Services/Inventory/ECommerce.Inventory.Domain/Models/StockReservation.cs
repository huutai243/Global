namespace ECommerce.Inventory.Domain.Models;

public sealed class StockReservation
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid CustomerId { get; set; }

    public StockReservationStatus Status { get; set; }

    public string? FailureReason { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    // TODO RECONCILIATION:
    // Add jobs that find reservations left Reserved without matching Ordering/Payment progress.
    // The job should compare Ordering, Inventory, Payment, Outbox, and Inbox state before release or escalation.
    public DateTime? ConfirmedAtUtc { get; set; }

    public DateTime? ReleasedAtUtc { get; set; }

    public ICollection<StockReservationItem> Items { get; set; } = [];
}
