using ECommerce.Ordering.Domain.Models;
using ECommerce.Ordering.Infrastructure.Persistence;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Inbox;
using ECommerce.Shared.Messaging;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Ordering.Application.InventoryReservation;

public sealed class InventoryReservationResultHandler(
    OrderingDbContext dbContext,
    IMessageNameResolver messageNameResolver,
    ILogger<InventoryReservationResultHandler> logger)
{
    private const string ConsumerName = "Ordering.InventoryReservationResult";

    public async Task HandleReservedAsync(
        InventoryReservedEvent message,
        MessageMetadata metadata,
        string payload,
        CancellationToken cancellationToken)
    {
        await HandleAsync(
            orderId: message.OrderId,
            metadata: metadata,
            payload: payload,
            messageType: messageNameResolver.ResolveMessageName(typeof(InventoryReservedEvent)),
            targetStatus: OrderStatus.PendingPayment,
            cancellationToken: cancellationToken);
    }

    public async Task HandleFailedAsync(
        InventoryReservationFailedEvent message,
        MessageMetadata metadata,
        string payload,
        CancellationToken cancellationToken)
    {
        await HandleAsync(
            orderId: message.OrderId,
            metadata: metadata,
            payload: payload,
            messageType: messageNameResolver.ResolveMessageName(typeof(InventoryReservationFailedEvent)),
            targetStatus: OrderStatus.InventoryFailed,
            cancellationToken: cancellationToken);
    }

    private async Task HandleAsync(
        Guid orderId,
        MessageMetadata metadata,
        string payload,
        string messageType,
        OrderStatus targetStatus,
        CancellationToken cancellationToken)
    {
        if (await IsProcessedAsync(metadata.MessageId, cancellationToken))
        {
            logger.LogInformation(
                "Inventory reservation result already processed. MessageId: {MessageId}, OrderId: {OrderId}",
                metadata.MessageId,
                orderId);

            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var inboxMessage = CreateInboxMessage(metadata, payload, messageType);

            dbContext.InboxMessages.Add(inboxMessage);

            var order = await dbContext.Orders
                .FirstOrDefaultAsync(
                    order => order.Id == orderId,
                    cancellationToken);

            if (order is null)
            {
                MarkInboxFailed(inboxMessage, $"Order '{orderId}' was not found.");

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                logger.LogWarning(
                    "Inventory reservation result ignored because order was not found. MessageId: {MessageId}, OrderId: {OrderId}",
                    metadata.MessageId,
                    orderId);

                return;
            }

            if (order.Status != OrderStatus.PendingInventoryReservation)
            {
                MarkInboxProcessed(inboxMessage);

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation(
                    "Inventory reservation result ignored because order status is not pending inventory reservation. MessageId: {MessageId}, OrderId: {OrderId}, CurrentStatus: {CurrentStatus}",
                    metadata.MessageId,
                    orderId,
                    order.Status);

                return;
            }

            order.Status = targetStatus;

            MarkInboxProcessed(inboxMessage);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Order status updated from inventory reservation result. MessageId: {MessageId}, OrderId: {OrderId}, NewStatus: {NewStatus}",
                metadata.MessageId,
                orderId,
                targetStatus);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);

            logger.LogInformation(
                exception,
                "Inventory reservation result skipped by idempotency constraint. MessageId: {MessageId}, OrderId: {OrderId}",
                metadata.MessageId,
                orderId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private InboxMessage CreateInboxMessage(
        MessageMetadata metadata,
        string payload,
        string messageType)
    {
        var utcNow = DateTime.UtcNow;

        return new InboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = metadata.MessageId,
            CorrelationId = metadata.CorrelationId,
            CausationId = metadata.CausationId,
            MessageType = messageType,
            ConsumerName = ConsumerName,
            Payload = payload,
            Status = InboxMessageStatus.Processing,
            ReceivedAtUtc = utcNow,
            ProcessingStartedAtUtc = utcNow
        };
    }

    private async Task<bool> IsProcessedAsync(
        string messageId,
        CancellationToken cancellationToken)
    {
        return await dbContext.InboxMessages.AnyAsync(
            message => message.MessageId == messageId
                && message.ConsumerName == ConsumerName
                && message.Status == InboxMessageStatus.Processed,
            cancellationToken);
    }

    private static void MarkInboxProcessed(InboxMessage inboxMessage)
    {
        inboxMessage.Status = InboxMessageStatus.Processed;
        inboxMessage.ProcessedAtUtc = DateTime.UtcNow;
        inboxMessage.ErrorMessage = null;
    }

    private static void MarkInboxFailed(
        InboxMessage inboxMessage,
        string errorMessage)
    {
        inboxMessage.Status = InboxMessageStatus.Failed;
        inboxMessage.ErrorMessage = errorMessage;
        inboxMessage.ProcessedAtUtc = DateTime.UtcNow;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}