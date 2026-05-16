using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Domain.Core.Inventory.Models;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Domain.Service.Inventory.AdjustInventory;

public sealed class AdjustInventoryCommandHandler(
    ECommerceDbContext dbContext,
    ILogger<AdjustInventoryCommandHandler> logger)
    : IRequestHandler<AdjustInventoryCommand, InventoryResponse>
{
    public async Task<InventoryResponse> Handle(AdjustInventoryCommand request, CancellationToken cancellationToken)
    {
        var inventoryItem = await dbContext.InventoryItems
            .FirstOrDefaultAsync(item => item.ProductId == request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Inventory item was not found.");

        dbContext.Entry(inventoryItem).Property(item => item.RowVersion).OriginalValue = request.RowVersion;

        var quantityAfterChange = inventoryItem.AvailableQuantity + request.QuantityChanged;
        if (quantityAfterChange < 0)
        {
            throw new BusinessRuleException("Available quantity cannot be negative.");
        }

        inventoryItem.AvailableQuantity = quantityAfterChange;
        inventoryItem.UpdatedAt = DateTime.UtcNow;

        dbContext.InventoryTransactions.Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            QuantityChanged = request.QuantityChanged,
            QuantityAfterChange = quantityAfterChange,
            Reason = request.Reason,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogWarning(exception, "Optimistic concurrency conflict for product {ProductId}", request.ProductId);
            throw new ConcurrencyException("Inventory was changed by another request. Reload and retry.");
        }

        return new InventoryResponse(inventoryItem.ProductId, inventoryItem.AvailableQuantity, inventoryItem.RowVersion);
    }
}
