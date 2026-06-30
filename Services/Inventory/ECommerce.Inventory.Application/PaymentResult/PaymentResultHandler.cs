using ECommerce.Inventory.Domain.Models;
using ECommerce.Inventory.Infrastructure.Persistence;
using ECommerce.Shared.Contracts.Payment;
using ECommerce.Shared.Inbox;
using ECommerce.Shared.Messaging;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Inventory.Application.PaymentResult;

public sealed class PaymentResultHandler(
    InventoryDbContext dbContext,
    IMessageNameResolver messageNameResolver,
    ILogger<PaymentResultHandler> logger)
{
    private const string ConsumerName = "Inventory.PaymentResult";
    private const int MaxConcurrencyRetryCount = 3;

    public async Task HandleSucceededAsync(
        PaymentSucceededEvent @event,
        MessageMetadata metadata,
        string payload,
        CancellationToken cancellationToken)
    {
        await HandleWithConcurrencyRetryAsync(
            () => ProcessSucceededAsync(@event, metadata, payload, cancellationToken),
            metadata.MessageId,
            @event.OrderId,
            cancellationToken);
    }

    public async Task HandleFailedAsync(
        PaymentFailedEvent @event,
        MessageMetadata metadata,
        string payload,
        CancellationToken cancellationToken)
    {
        await HandleWithConcurrencyRetryAsync(
            () => ProcessFailedAsync(@event, metadata, payload, cancellationToken),
            metadata.MessageId,
            @event.OrderId,
            cancellationToken);
    }

    private async Task HandleWithConcurrencyRetryAsync(
        Func<Task> process,
        string messageId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyRetryCount; attempt++)
        {
            try
            {
                await process();
                return;
            }
            catch (DbUpdateConcurrencyException exception) when (attempt < MaxConcurrencyRetryCount)
            {
                dbContext.ChangeTracker.Clear();

                logger.LogWarning(
                    exception,
                    "Payment inventory result concurrency conflict. Retrying. Attempt: {Attempt}, MessageId: {MessageId}, OrderId: {OrderId}",
                    attempt,
                    messageId,
                    orderId);

                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), cancellationToken);
            }
        }
    }

    private async Task ProcessSucceededAsync(
        PaymentSucceededEvent @event,
        MessageMetadata metadata,
        string payload,
        CancellationToken cancellationToken)
    {
        await ProcessAsync(
            @event,
            metadata,
            payload,
            confirmReservation: true,
            failureReason: null,
            cancellationToken);
    }

    private async Task ProcessFailedAsync(
        PaymentFailedEvent @event,
        MessageMetadata metadata,
        string payload,
        CancellationToken cancellationToken)
    {
        await ProcessAsync(
            @event,
            metadata,
            payload,
            confirmReservation: false,
            failureReason: @event.FailureReason,
            cancellationToken);
    }

    private async Task ProcessAsync<TEvent>(
        TEvent @event,
        MessageMetadata metadata,
        string payload,
        bool confirmReservation,
        string? failureReason,
        CancellationToken cancellationToken)
        where TEvent : class
    {
        var orderId = GetOrderId(@event);

        if (await IsProcessedAsync(metadata.MessageId, cancellationToken))
        {
            logger.LogInformation(
                "Payment inventory result message already processed. MessageId: {MessageId}, OrderId: {OrderId}",
                metadata.MessageId,
                orderId);

            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var inboxMessage = CreateInboxMessage<TEvent>(metadata, payload);
            dbContext.InboxMessages.Add(inboxMessage);

            var reservation = await GetReservationAsync(orderId, cancellationToken);

            if (confirmReservation)
            {
                await ConfirmReservationAsync(reservation, cancellationToken);
            }
            else
            {
                await ReleaseReservationAsync(reservation, failureReason, cancellationToken);
            }

            MarkInboxProcessed(inboxMessage);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Payment inventory result applied. MessageId: {MessageId}, OrderId: {OrderId}, ReservationStatus: {ReservationStatus}",
                metadata.MessageId,
                orderId,
                reservation.Status);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);

            logger.LogInformation(
                exception,
                "Payment inventory result skipped by idempotency constraint. MessageId: {MessageId}, OrderId: {OrderId}",
                metadata.MessageId,
                orderId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<StockReservation> GetReservationAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var reservation = await dbContext.StockReservations
            .Include(reservation => reservation.Items)
            .FirstOrDefaultAsync(
                reservation => reservation.OrderId == orderId,
                cancellationToken);

        if (reservation is null)
        {
            throw new InvalidOperationException(
                $"Stock reservation was not found for payment result. OrderId: {orderId}");
        }

        return reservation;
    }

    private async Task ConfirmReservationAsync(
        StockReservation reservation,
        CancellationToken cancellationToken)
    {
        if (reservation.Status == StockReservationStatus.Confirmed)
        {
            logger.LogInformation(
                "Stock reservation confirmation ignored because reservation is already confirmed. OrderId: {OrderId}",
                reservation.OrderId);

            return;
        }

        if (reservation.Status != StockReservationStatus.Reserved)
        {
            throw new InvalidOperationException(
                $"Cannot confirm stock reservation for order '{reservation.OrderId}' because current status is '{reservation.Status}'.");
        }

        var inventoryItems = await GetInventoryItemsAsync(reservation, cancellationToken);
        var utcNow = DateTime.UtcNow;

        foreach (var reservationItem in reservation.Items)
        {
            var inventoryItem = inventoryItems[reservationItem.ProductId];

            if (inventoryItem.ReservedQuantity < reservationItem.Quantity)
            {
                throw new InvalidOperationException(
                    $"Cannot confirm stock reservation for product '{reservationItem.ProductId}' because reserved quantity is insufficient.");
            }

            // Reservation already reduced AvailableQuantity during reserve.
            // Payment success finalizes the sale by removing the held quantity from ReservedQuantity.
            inventoryItem.ReservedQuantity -= reservationItem.Quantity;
            inventoryItem.UpdatedAtUtc = utcNow;
        }

        reservation.Status = StockReservationStatus.Confirmed;
        reservation.FailureReason = null;
    }

    private async Task ReleaseReservationAsync(
        StockReservation reservation,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        if (reservation.Status == StockReservationStatus.Released)
        {
            logger.LogInformation(
                "Stock reservation release ignored because reservation is already released. OrderId: {OrderId}",
                reservation.OrderId);

            return;
        }

        if (reservation.Status == StockReservationStatus.Failed)
        {
            logger.LogInformation(
                "Stock reservation release ignored because reservation already failed during reservation. OrderId: {OrderId}",
                reservation.OrderId);

            return;
        }

        if (reservation.Status != StockReservationStatus.Reserved)
        {
            throw new InvalidOperationException(
                $"Cannot release stock reservation for order '{reservation.OrderId}' because current status is '{reservation.Status}'.");
        }

        var inventoryItems = await GetInventoryItemsAsync(reservation, cancellationToken);
        var utcNow = DateTime.UtcNow;

        foreach (var reservationItem in reservation.Items)
        {
            var inventoryItem = inventoryItems[reservationItem.ProductId];

            if (inventoryItem.ReservedQuantity < reservationItem.Quantity)
            {
                throw new InvalidOperationException(
                    $"Cannot release stock reservation for product '{reservationItem.ProductId}' because reserved quantity is insufficient.");
            }

            inventoryItem.AvailableQuantity += reservationItem.Quantity;
            inventoryItem.ReservedQuantity -= reservationItem.Quantity;
            inventoryItem.UpdatedAtUtc = utcNow;
        }

        reservation.Status = StockReservationStatus.Released;
        reservation.FailureReason = failureReason;
    }

    private async Task<Dictionary<Guid, InventoryItem>> GetInventoryItemsAsync(
        StockReservation reservation,
        CancellationToken cancellationToken)
    {
        var productIds = reservation.Items
            .Select(item => item.ProductId)
            .ToArray();

        var inventoryItems = await dbContext.InventoryItems
            .Where(item => productIds.Contains(item.ProductId))
            .ToDictionaryAsync(item => item.ProductId, cancellationToken);

        var missingProductId = productIds.FirstOrDefault(
            productId => !inventoryItems.ContainsKey(productId));

        if (missingProductId != Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Inventory item was not found for product '{missingProductId}'.");
        }

        return inventoryItems;
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