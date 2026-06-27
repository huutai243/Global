using ECommerce.Inventory.Domain.Models;
using ECommerce.Inventory.Infrastructure.Persistence;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Inbox;
using ECommerce.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Inventory.Application.ProductCreated;

public sealed class ProductCreatedEventHandler(
    InventoryDbContext dbContext,
    ILogger<ProductCreatedEventHandler> logger)
{
    private const string ConsumerName = "Inventory.ProductCreatedConsumer";

    public async Task HandleAsync(
        ProductCreatedEvent message,
        MessageMetadata metadata,
        string payload,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var exists = await dbContext.InventoryItems
                .AnyAsync(
                    item => item.ProductId == message.ProductId,
                    cancellationToken);

            if (!exists)
            {
                var inventoryItem = new InventoryItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = message.ProductId,
                    AvailableQuantity = message.InitialStock,
                    ReservedQuantity = 0,
                    CreatedAtUtc = message.OccurredAtUtc
                };

                dbContext.InventoryItems.Add(inventoryItem);
            }

            dbContext.InboxMessages.Add(new InboxMessage
            {
                Id = Guid.NewGuid(),
                MessageId = metadata.MessageId,
                CorrelationId = metadata.CorrelationId,
                CausationId = metadata.CausationId,
                ConsumerName = ConsumerName,
                MessageType = typeof(ProductCreatedEvent).FullName!,
                Payload = payload,
                Status = InboxMessageStatus.Processed,
                ReceivedAtUtc = DateTime.UtcNow,
                ProcessedAtUtc = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Inventory item initialized for product. ProductId: {ProductId}, InitialStock: {InitialStock}, MessageId: {MessageId}",
                message.ProductId,
                message.InitialStock,
                metadata.MessageId);
        }
        catch (DbUpdateException exception) when (IsDuplicateInboxMessage(exception))
        {
            await transaction.RollbackAsync(cancellationToken);

            logger.LogInformation(
                "ProductCreatedEvent already processed. MessageId: {MessageId}, ProductId: {ProductId}",
                metadata.MessageId,
                message.ProductId);
        }
    }

    private static bool IsDuplicateInboxMessage(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;

        return message.Contains("IX_InboxMessages_MessageId_ConsumerName", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }
}