using ECommerce.Shared.Contracts;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Worker.Consumers;

public sealed class ConfirmInventoryReservationCommandConsumer(
    ECommerceDbContext dbContext,
    ILogger<ConfirmInventoryReservationCommandConsumer> logger)
{
    public async Task HandleAsync(ConfirmInventoryReservationCommand command, CancellationToken cancellationToken = default)
    {
        if (await HasProcessedAsync(nameof(ConfirmInventoryReservationCommand), command.OrderId, cancellationToken))
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var order = await dbContext.Orders
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == command.OrderId, cancellationToken);

        if (order is not null)
        {
            var productIds = order.Items.Select(item => item.ProductId).ToArray();
            var inventoryItems = await dbContext.InventoryItems
                .Where(item => productIds.Contains(item.ProductId))
                .ToDictionaryAsync(item => item.ProductId, cancellationToken);

            foreach (var orderItem in order.Items)
            {
                if (!inventoryItems.TryGetValue(orderItem.ProductId, out var inventoryItem))
                {
                    continue;
                }

                inventoryItem.ReservedQuantity = Math.Max(0, inventoryItem.ReservedQuantity - orderItem.Quantity);
                inventoryItem.SoldQuantity += orderItem.Quantity;
                inventoryItem.UpdatedAt = DateTime.UtcNow;
            }
        }

        MarkProcessed(nameof(ConfirmInventoryReservationCommand), command.OrderId);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Confirmed inventory reservation for order {OrderId}", command.OrderId);
    }

    private async Task<bool> HasProcessedAsync(string messageType, Guid aggregateId, CancellationToken cancellationToken)
    {
        var key = BuildKey(messageType, aggregateId);
        return await dbContext.IdempotencyRecords.AnyAsync(record => record.Key == key, cancellationToken);
    }

    private void MarkProcessed(string messageType, Guid aggregateId)
    {
        dbContext.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Key = BuildKey(messageType, aggregateId),
            RequestHash = aggregateId.ToString("N"),
            Status = "Completed",
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        });
    }

    private static string BuildKey(string messageType, Guid aggregateId) => $"Inventory:{messageType}:{aggregateId:N}";
}
