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

    public async Task HandleAsync(
        ReserveInventoryCommand command,
        MessageMetadata metadata,
        string payload,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

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
            ReceivedAtUtc = DateTime.UtcNow,
            ProcessingStartedAtUtc = DateTime.UtcNow
        };
    }

    private async Task<bool> IsProcessedAsync(string messageId, CancellationToken cancellationToken)
    {
        return await dbContext.InboxMessages.AnyAsync(
            x => x.MessageId == messageId &&
                 x.ConsumerName == ConsumerName &&
                 x.Status == InboxMessageStatus.Processed,
            cancellationToken);
    }

    private async Task<bool> HasReservationAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await dbContext.StockReservations.AnyAsync(
            x => x.OrderId == orderId,
            cancellationToken);
    }

    private async Task<Dictionary<Guid, InventoryItem>> GetInventoryItemsAsync(
        ReserveInventoryCommand command,
        CancellationToken cancellationToken)
    {
        var productIds = command.Items.Select(x => x.ProductId).ToArray();

        return await dbContext.InventoryItems
            .Where(x => productIds.Contains(x.ProductId))
            .ToDictionaryAsync(x => x.ProductId, cancellationToken);
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
            .Where(x => x.IsFailed)
            .Select(x => new InventoryReservationFailedItem(
                x.Item.ProductId,
                x.Item.ProductName,
                x.Item.Quantity,
                x.AvailableQuantity))
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

        var reservation = CreateReservation(command, StockReservationStatus.Reserved, null, utcNow);

        var @event = new InventoryReservedEvent(
            command.OrderId,
            command.CustomerId,
            command.Items.Select(x => new InventoryReservedItem(x.ProductId, x.Quantity)).ToArray(),
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
            metadata.CorrelationId,
            metadata.MessageId,
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