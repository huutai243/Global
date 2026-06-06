using System.Text.Json;
using ECommerce.Shared.Contracts;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Worker.Consumers;

public sealed class ReserveInventoryCommandConsumer(ECommerceDbContext dbContext, ILogger<ReserveInventoryCommandConsumer> logger)
{
    public async Task HandleAsync(ReserveInventoryCommand command, CancellationToken cancellationToken = default)
    {
        if (await HasProcessedAsync(nameof(ReserveInventoryCommand), command.OrderId, cancellationToken))
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var productIds = command.Items.Select(item => item.ProductId).ToArray();
        var inventoryItems = await dbContext.InventoryItems
            .Where(item => productIds.Contains(item.ProductId))
            .ToDictionaryAsync(item => item.ProductId, cancellationToken);

        var failedItem = command.Items.FirstOrDefault(item =>
            !inventoryItems.TryGetValue(item.ProductId, out var inventoryItem) ||
            inventoryItem.AvailableQuantity < item.Quantity);

        object message;

        if (failedItem is null)
        {
            foreach (var item in command.Items)
            {
                var inventoryItem = inventoryItems[item.ProductId];
                inventoryItem.AvailableQuantity -= item.Quantity;
                inventoryItem.ReservedQuantity += item.Quantity;
                inventoryItem.UpdatedAt = DateTime.UtcNow;
            }

            message = new InventoryReservedEvent(command.OrderId, command.CustomerId, DateTime.UtcNow);
            logger.LogInformation("Reserved inventory for order {OrderId}", command.OrderId);
        }
        else
        {
            message = new InventoryReserveFailedEvent(
                command.OrderId,
                command.CustomerId,
                $"Insufficient stock for product {failedItem.ProductId}.",
                DateTime.UtcNow);
            logger.LogWarning("Inventory reservation failed for order {OrderId}", command.OrderId);
        }

        AddOutboxMessage(message);
        MarkProcessed(nameof(ReserveInventoryCommand), command.OrderId);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
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

    private void AddOutboxMessage(object message)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = message.GetType().Name,
            Payload = JsonSerializer.Serialize(message),
            Status = OutboxStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static string BuildKey(string messageType, Guid aggregateId) => $"Inventory:{messageType}:{aggregateId:N}";
}
