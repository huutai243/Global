using System.Text.Json;
using ECommerce.Shared.Contracts;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Models;
using ECommerce.Ordering.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Worker.Consumers;

public sealed class InventoryReserveFailedEventConsumer(ECommerceDbContext dbContext, ILogger<InventoryReserveFailedEventConsumer> logger)
{
    public async Task HandleAsync(InventoryReserveFailedEvent message, CancellationToken cancellationToken = default)
    {
        if (await HasProcessedAsync(nameof(InventoryReserveFailedEvent), message.OrderId, cancellationToken))
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var order = await dbContext.Orders.FirstOrDefaultAsync(item => item.Id == message.OrderId, cancellationToken);
        if (order is not null && order.Status == OrderStatus.PendingInventoryReservation)
        {
            order.Status = OrderStatus.InventoryFailed;
            order.UpdatedAt = DateTime.UtcNow;

            AddOutboxMessage(new OrderCancelledEvent(order.Id, order.CustomerId, message.Reason, DateTime.UtcNow));
        }

        MarkProcessed(nameof(InventoryReserveFailedEvent), message.OrderId);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Ordering handled inventory failure for order {OrderId}", message.OrderId);
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

    private static string BuildKey(string messageType, Guid aggregateId) => $"Ordering:{messageType}:{aggregateId:N}";
}
