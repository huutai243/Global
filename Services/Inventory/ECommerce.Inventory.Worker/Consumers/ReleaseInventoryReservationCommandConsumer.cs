using ECommerce.Shared.Contracts;
using ECommerce.Inventory.Infrastructure.Persistence;
using ECommerce.Shared.Inbox;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Worker.Consumers;

public sealed class ReleaseInventoryReservationCommandConsumer(
    InventoryDbContext dbContext,
    ILogger<ReleaseInventoryReservationCommandConsumer> logger)
{
    public async Task HandleAsync(ReleaseInventoryReservationCommand command, CancellationToken cancellationToken = default)
    {
        if (await HasProcessedAsync(nameof(ReleaseInventoryReservationCommand), command.OrderId, cancellationToken))
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // TODO: Boundary violation removed. Release reservation must use reservation state or message payload,
        // not Ordering tables, once InventoryReservation is introduced.

        MarkProcessed(nameof(ReleaseInventoryReservationCommand), command.OrderId);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Released inventory reservation for order {OrderId}", command.OrderId);
    }

    private async Task<bool> HasProcessedAsync(string messageType, Guid aggregateId, CancellationToken cancellationToken)
    {
        var key = BuildKey(messageType, aggregateId);
        return await dbContext.InboxMessages.AnyAsync(
            record => record.MessageId == key && record.ConsumerName == nameof(ReleaseInventoryReservationCommandConsumer),
            cancellationToken);
    }

    private void MarkProcessed(string messageType, Guid aggregateId)
    {
        dbContext.InboxMessages.Add(new InboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = BuildKey(messageType, aggregateId),
            CorrelationId = aggregateId.ToString("N"),
            CausationId = aggregateId.ToString("N"),
            MessageType = messageType,
            ConsumerName = nameof(ReleaseInventoryReservationCommandConsumer),
            Payload = string.Empty,
            Status = InboxMessageStatus.Processed,
            ReceivedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = DateTime.UtcNow
        });
    }

    private static string BuildKey(string messageType, Guid aggregateId) => $"Inventory:{messageType}:{aggregateId:N}";
}
