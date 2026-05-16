namespace ECommerce.Domain.Service.Inventory.AdjustInventory;

public sealed record InventoryResponse(Guid ProductId, int AvailableQuantity, byte[] RowVersion);
