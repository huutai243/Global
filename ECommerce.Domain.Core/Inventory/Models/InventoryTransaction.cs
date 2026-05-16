namespace ECommerce.Domain.Core.Inventory.Models;

public class InventoryTransaction
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public int QuantityChanged { get; set; }

    public int QuantityAfterChange { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
