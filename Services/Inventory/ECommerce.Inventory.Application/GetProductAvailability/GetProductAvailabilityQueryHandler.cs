using ECommerce.Inventory.Infrastructure.Persistence;
using ECommerce.Shared.Core.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Application.GetProductAvailability;

public sealed class GetProductAvailabilityQueryHandler(
    InventoryDbContext dbContext)
    : IRequestHandler<GetProductAvailabilityQuery, ProductAvailabilityResponse>
{
    public async Task<ProductAvailabilityResponse> Handle(
        GetProductAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        var inventoryItem = await dbContext.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ProductId == request.ProductId,
                cancellationToken);

        if (inventoryItem is null)
        {
            throw new NotFoundException("Inventory item was not found.");
        }

        var stockStatus = GetStockStatus(inventoryItem.AvailableQuantity);

        return new ProductAvailabilityResponse(
            inventoryItem.ProductId,
            inventoryItem.AvailableQuantity,
            inventoryItem.ReservedQuantity,
            stockStatus);
    }

    private static string GetStockStatus(int availableQuantity)
    {
        if (availableQuantity <= 0)
        {
            return "OutOfStock";
        }

        if (availableQuantity <= 5)
        {
            return "LowStock";
        }

        return "InStock";
    }
}