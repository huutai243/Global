namespace ECommerce.Inventory.Application.AdjustInventory;

public sealed record InventoryResponse(Guid ProductId, int AvailableQuantity, byte[] RowVersion);
