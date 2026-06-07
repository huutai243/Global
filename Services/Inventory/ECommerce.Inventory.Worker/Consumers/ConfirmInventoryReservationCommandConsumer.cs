using ECommerce.Shared.Contracts;
using ECommerce.Inventory.Infrastructure.Persistence;
using ECommerce.Shared.Inbox;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Worker.Consumers;

public sealed class ConfirmInventoryReservationCommandConsumer(
    InventoryDbContext dbContext,
    ILogger<ConfirmInventoryReservationCommandConsumer> logger)
{
    public async Task HandleAsync(ConfirmInventoryReservationCommand command, CancellationToken cancellationToken = default)
    {
        if (await HasProcessedAsync(nameof(ConfirmInventoryReservationCommand), command.OrderId, cancellationToken))
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // TODO: Boundary violation removed. Confirm reservation must use reservation state or message payload,
        // not Ordering tables, once InventoryReservation is introduced.

        MarkProcessed(nameof(ConfirmInventoryReservationCommand), command.OrderId);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Confirmed inventory reservation for order {OrderId}", command.OrderId);
    }

    private async Task<bool> HasProcessedAsync(string messageType, Guid aggregateId, CancellationToken cancellationToken)
    {
        var key = BuildKey(messageType, aggregateId);
        return await dbContext.InboxMessages.AnyAsync(
            record => record.MessageId == key && record.ConsumerName == nameof(ConfirmInventoryReservationCommandConsumer),
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
            ConsumerName = nameof(ConfirmInventoryReservationCommandConsumer),
            Payload = string.Empty,
            Status = InboxMessageStatus.Processed,
            ReceivedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = DateTime.UtcNow
        });
    }

    private static string BuildKey(string messageType, Guid aggregateId) => $"Inventory:{messageType}:{aggregateId:N}";
}
