namespace ECommerce.Inventory.Domain.Models;

public sealed class InventoryItem
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public int AvailableQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}