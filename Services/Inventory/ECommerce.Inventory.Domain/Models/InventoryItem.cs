namespace ECommerce.Inventory.Domain.Models;

public sealed class InventoryItem
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public int AvailableQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    // STRONG CONSISTENCY NOTE:
    // This concurrency token is part of service-local stock consistency.
    // Cross-service consistency still depends on idempotent consumers and asynchronous reconciliation.
    public byte[] RowVersion { get; set; } = [];
}
