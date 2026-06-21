using ECommerce.Infrastructure.BackgroundJobs;
using ECommerce.Inventory.Infrastructure.Persistence;
using ECommerce.Inventory.Worker.Options;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Messaging;
using ECommerce.Shared.Outbox;
using Microsoft.Extensions.Options;

namespace ECommerce.Inventory.Worker;

public sealed class OutboxProcessor(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<OutboxOptions> options,
    IJsonHelper jsonHelper,
    ILogger<OutboxProcessor> logger)
    : OutboxProcessorBase<OutboxProcessor, InventoryDbContext, OutboxOptions>(
        serviceScopeFactory,
        options,
        logger)
{
    protected override string ProcessorName => "Inventory";

    protected override async Task PublishAsync(
        IMessagePublisher publisher,
        IMessageNameResolver messageNameResolver,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        var inventoryReservedEventType = messageNameResolver.ResolveMessageName(typeof(InventoryReservedEvent));

        if (string.Equals(message.MessageType, inventoryReservedEventType, StringComparison.Ordinal))
        {
            var @event = jsonHelper.DeserializeRequired<InventoryReservedEvent>(message.Payload, $"Inventory outbox payload for message '{message.MessageId}' could not be deserialized.");
            var metadata = OutboxMessageMetadataFactory.Create(message);

            await publisher.PublishAsync(@event, metadata, cancellationToken);
            return;
        }

        var inventoryReservationFailedEventType = messageNameResolver.ResolveMessageName(typeof(InventoryReservationFailedEvent));

        if (string.Equals(message.MessageType, inventoryReservationFailedEventType, StringComparison.Ordinal))
        {
            var @event = jsonHelper.DeserializeRequired<InventoryReservationFailedEvent>(message.Payload, $"Inventory outbox payload for message '{message.MessageId}' could not be deserialized.");
            var metadata = OutboxMessageMetadataFactory.Create(message);

            await publisher.PublishAsync(@event, metadata, cancellationToken);
            return;
        }

        throw new InvalidOperationException($"Unsupported inventory outbox message type '{message.MessageType}'.");
    }
}