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
        if (await IsProcessedAsync(metadata.MessageId, cancellationToken))
        {
            logger.LogInformation(
                "ProductCreatedEvent already processed. MessageId: {MessageId}, ProductId: {ProductId}",
                metadata.MessageId,
                message.ProductId);

            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

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

    private async Task<bool> IsProcessedAsync(
        string messageId,
        CancellationToken cancellationToken)
    {
        return await dbContext.InboxMessages.AnyAsync(
            message => message.MessageId == messageId
                && message.ConsumerName == ConsumerName,
            cancellationToken);
    }
}