using MediatR;

namespace ECommerce.Domain.Service.Inventory.AdjustInventory;

public sealed record AdjustInventoryCommand(Guid ProductId, int QuantityChanged, string Reason, byte[] RowVersion)
    : IRequest<InventoryResponse>;
