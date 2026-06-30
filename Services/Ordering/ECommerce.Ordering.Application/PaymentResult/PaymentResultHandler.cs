using ECommerce.Ordering.Domain.Models;
using ECommerce.Ordering.Infrastructure.Persistence;
using ECommerce.Shared.Contracts.Payment;
using ECommerce.Shared.Inbox;
using ECommerce.Shared.Messaging;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Ordering.Application.PaymentResult;

public sealed class PaymentResultHandler(
    OrderingDbContext dbContext,
    IMessageNameResolver messageNameResolver,
    ILogger<PaymentResultHandler> logger)
{
    private const string ConsumerName = "Ordering.PaymentResult";

    public async Task HandleSucceededAsync(
        PaymentSucceededEvent @event,
        MessageMetadata metadata,
        string payload,
        CancellationToken cancellationToken)
    {
        await ProcessAsync(
            @event,
            metadata,
            payload,
            OrderStatus.Paid,
            cancellationToken);
    }

    public async Task HandleFailedAsync(
        PaymentFailedEvent @event,
        MessageMetadata metadata,
        string payload,
        CancellationToken cancellationToken)
    {
        await ProcessAsync(
            @event,
            metadata,
            payload,
            OrderStatus.PaymentFailed,
            cancellationToken);
    }

    private async Task ProcessAsync<TEvent>(
        TEvent @event,
        MessageMetadata metadata,
        string payload,
        OrderStatus targetStatus,
        CancellationToken cancellationToken)
        where TEvent : class
    {
        var orderId = GetOrderId(@event);

        if (await IsProcessedAsync(metadata.MessageId, cancellationToken))
        {
            logger.LogInformation(
                "Payment result message already processed. MessageId: {MessageId}, OrderId: {OrderId}",
                metadata.MessageId,
                orderId);

            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var inboxMessage = CreateInboxMessage<TEvent>(metadata, payload);
            dbContext.InboxMessages.Add(inboxMessage);

            var order = await dbContext.Orders.FirstOrDefaultAsync(
                order => order.Id == orderId,
                cancellationToken);

            if (order is null)
            {
                throw new InvalidOperationException(
                    $"Order was not found for payment result. OrderId: {orderId}");
            }

            ApplyPaymentResult(order, targetStatus);

            MarkInboxProcessed(inboxMessage);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Payment result applied to order. MessageId: {MessageId}, OrderId: {OrderId}, Status: {Status}",
                metadata.MessageId,
                order.Id,
                order.Status);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);

            logger.LogInformation(
                exception,
                "Payment result message skipped by idempotency constraint. MessageId: {MessageId}, OrderId: {OrderId}",
                metadata.MessageId,
                orderId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private void ApplyPaymentResult(Order order, OrderStatus targetStatus)
    {
        if (order.Status == targetStatus)
        {
            logger.LogInformation(
                "Payment result ignored because order is already in target status. OrderId: {OrderId}, Status: {Status}",
                order.Id,
                order.Status);

            return;
        }

        if (order.Status != OrderStatus.PendingPayment)
        {
            throw new InvalidOperationException(
                $"Cannot apply payment result to order '{order.Id}' because current status is '{order.Status}'.");
        }

        order.Status = targetStatus;
    }

    private InboxMessage CreateInboxMessage<TEvent>(
        MessageMetadata metadata,
        string payload)
        where TEvent : class
    {
        var utcNow = DateTime.UtcNow;

        return new InboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = metadata.MessageId,
            CorrelationId = metadata.CorrelationId,
            CausationId = metadata.CausationId,
            MessageType = messageNameResolver.ResolveMessageName(typeof(TEvent)),
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

    private static Guid GetOrderId<TEvent>(TEvent @event)
        where TEvent : class
    {
        return @event switch
        {
            PaymentSucceededEvent succeeded => succeeded.OrderId,
            PaymentFailedEvent failed => failed.OrderId,
            _ => throw new InvalidOperationException(
                $"Unsupported payment result event type '{typeof(TEvent).Name}'.")
        };
    }

    private static void MarkInboxProcessed(InboxMessage inboxMessage)
    {
        inboxMessage.Status = InboxMessageStatus.Processed;
        inboxMessage.ProcessedAtUtc = DateTime.UtcNow;
        inboxMessage.ErrorMessage = null;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}