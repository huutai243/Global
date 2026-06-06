using MediatR;

namespace ECommerce.Inventory.Application.AdjustInventory;

public sealed record AdjustInventoryCommand(Guid ProductId, int QuantityChanged, string Reason, byte[] RowVersion)
    : IRequest<InventoryResponse>;
