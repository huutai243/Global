using ECommerce.Inventory.Domain.Models;
using ECommerce.Inventory.Infrastructure.Persistence;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Inbox;
using ECommerce.Shared.Messaging;
using ECommerce.Shared.Outbox;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Inventory.Application.ReserveInventory;

public sealed class ReserveInventoryCommandHandler(
    InventoryDbContext dbContext,
    IValidator<ReserveInventoryCommand> validator,
    OutboxMessageFactory outboxMessageFactory,
    IMessageNameResolver messageNameResolver,
    ILogger<ReserveInventoryCommandHandler> logger)
{
    private const string ConsumerName = "Inventory.ReserveInventory";
    private const string SourceService = "Inventory";
    private const string DestinationService = "Ordering";
    private const int MaxConcurrencyRetryCount = 3;

    public async Task HandleAsync(
        ReserveInventoryCommand command,
        MessageMetadata metadata,
        string payload,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        for (var attempt = 1; attempt <= MaxConcurrencyRetryCount; attempt++)
        {
            try
            {
                await ProcessAsync(command, metadata, payload, cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException exception) when (attempt < MaxConcurrencyRetryCount)
            {
                dbContext.ChangeTracker.Clear();

                logger.LogWarning(
                    exception,
                    "Reserve inventory concurrency conflict. Retrying. Attempt: {Attempt}, MessageId: {MessageId}, OrderId: {OrderId}",
                    attempt,
                    metadata.MessageId,
                    command.OrderId);

                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), cancellationToken);
            }
        }
    }

    private async Task ProcessAsync(
        ReserveInventoryCommand command,
        MessageMetadata metadata,
        string payload,
        CancellationToken cancellationToken)
    {
        if (await IsProcessedAsync(metadata.MessageId, cancellationToken))
        {
            logger.LogInformation(
                "Reserve inventory message already processed. MessageId: {MessageId}, OrderId: {OrderId}",
                metadata.MessageId,
                command.OrderId);

            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var inboxMessage = CreateInboxMessage(metadata, payload);
            dbContext.InboxMessages.Add(inboxMessage);

            if (await HasReservationAsync(command.OrderId, cancellationToken))
            {
                MarkInboxProcessed(inboxMessage);

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation(
                    "Reserve inventory skipped because reservation already exists. MessageId: {MessageId}, OrderId: {OrderId}",
                    metadata.MessageId,
                    command.OrderId);

                return;
            }

            var inventoryItems = await GetInventoryItemsAsync(command, cancellationToken);
            var failedItems = GetFailedItems(command, inventoryItems);

            if (failedItems.Count > 0)
            {
                CreateFailedReservation(command, metadata, failedItems);
            }
            else
            {
                CreateSuccessfulReservation(command, metadata, inventoryItems);
            }

            MarkInboxProcessed(inboxMessage);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Reserve inventory command processed. MessageId: {MessageId}, OrderId: {OrderId}",
                metadata.MessageId,
                command.OrderId);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);

            logger.LogInformation(
                exception,
                "Reserve inventory message skipped by idempotency constraint. MessageId: {MessageId}, OrderId: {OrderId}",
                metadata.MessageId,
                command.OrderId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private InboxMessage CreateInboxMessage(MessageMetadata metadata, string payload)
    {
        var utcNow = DateTime.UtcNow;

        return new InboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = metadata.MessageId,
            CorrelationId = metadata.CorrelationId,
            CausationId = metadata.CausationId,
            MessageType = messageNameResolver.ResolveMessageName(typeof(ReserveInventoryCommand)),
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

    private async Task<bool> HasReservationAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        return await dbContext.StockReservations.AnyAsync(
            reservation => reservation.OrderId == orderId,
            cancellationToken);
    }

    private async Task<Dictionary<Guid, InventoryItem>> GetInventoryItemsAsync(
        ReserveInventoryCommand command,
        CancellationToken cancellationToken)
    {
        var productIds = command.Items
            .Select(item => item.ProductId)
            .ToArray();

        return await dbContext.InventoryItems
            .Where(item => productIds.Contains(item.ProductId))
            .ToDictionaryAsync(item => item.ProductId, cancellationToken);
    }

    private static List<InventoryReservationFailedItem> GetFailedItems(
        ReserveInventoryCommand command,
        IReadOnlyDictionary<Guid, InventoryItem> inventoryItems)
    {
        return command.Items
            .Select(item =>
            {
                inventoryItems.TryGetValue(item.ProductId, out var inventoryItem);

                var availableQuantity = inventoryItem?.AvailableQuantity ?? 0;

                return new
                {
                    Item = item,
                    AvailableQuantity = availableQuantity,
                    IsFailed = inventoryItem is null || availableQuantity < item.Quantity
                };
            })
            .Where(item => item.IsFailed)
            .Select(item => new InventoryReservationFailedItem(
                item.Item.ProductId,
                item.Item.ProductName,
                item.Item.Quantity,
                item.AvailableQuantity))
            .ToList();
    }

    private void CreateSuccessfulReservation(
        ReserveInventoryCommand command,
        MessageMetadata metadata,
        IReadOnlyDictionary<Guid, InventoryItem> inventoryItems)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var item in command.Items)
        {
            var inventoryItem = inventoryItems[item.ProductId];

            inventoryItem.AvailableQuantity -= item.Quantity;
            inventoryItem.ReservedQuantity += item.Quantity;
            inventoryItem.UpdatedAtUtc = utcNow;
        }

        var reservation = CreateReservation(
            command,
            StockReservationStatus.Reserved,
            null,
            utcNow);

        var @event = new InventoryReservedEvent(
            command.OrderId,
            command.CustomerId,
            command.Items
                .Select(item => new InventoryReservedItem(item.ProductId, item.Quantity))
                .ToArray(),
            utcNow);

        dbContext.StockReservations.Add(reservation);
        dbContext.OutboxMessages.Add(CreateOutboxMessage(@event, metadata, utcNow));
    }

    private void CreateFailedReservation(
        ReserveInventoryCommand command,
        MessageMetadata metadata,
        IReadOnlyCollection<InventoryReservationFailedItem> failedItems)
    {
        var utcNow = DateTime.UtcNow;

        var reservation = CreateReservation(
            command,
            StockReservationStatus.Failed,
            "Insufficient inventory.",
            utcNow);

        var @event = new InventoryReservationFailedEvent(
            command.OrderId,
            command.CustomerId,
            failedItems,
            "Insufficient inventory.",
            utcNow);

        dbContext.StockReservations.Add(reservation);
        dbContext.OutboxMessages.Add(CreateOutboxMessage(@event, metadata, utcNow));
    }

    private static StockReservation CreateReservation(
        ReserveInventoryCommand command,
        StockReservationStatus status,
        string? failureReason,
        DateTime utcNow)
    {
        return new StockReservation
        {
            Id = Guid.NewGuid(),
            OrderId = command.OrderId,
            CustomerId = command.CustomerId,
            Status = status,
            FailureReason = failureReason,
            CreatedAtUtc = utcNow,
            Items = command.Items.Select(item => new StockReservationItem
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                ProductNameSnapshot = item.ProductName,
                Quantity = item.Quantity
            }).ToList()
        };
    }

    private OutboxMessage CreateOutboxMessage<TEvent>(
        TEvent @event,
        MessageMetadata metadata,
        DateTime utcNow)
        where TEvent : class
    {
        return outboxMessageFactory.Create(
            @event,
            SourceService,
            DestinationService,
            Guid.NewGuid().ToString("N"),
            metadata.CorrelationId,
            utcNow);
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