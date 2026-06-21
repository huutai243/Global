using ECommerce.Infrastructure.BackgroundJobs;
using ECommerce.Ordering.Infrastructure.Persistence;
using ECommerce.Ordering.Worker.Options;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Messaging;
using ECommerce.Shared.Outbox;
using Microsoft.Extensions.Options;

namespace ECommerce.Ordering.Worker;

public sealed class OutboxProcessor(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<OutboxOptions> options,
    IJsonHelper jsonHelper,
    ILogger<OutboxProcessor> logger)
    : OutboxProcessorBase<OutboxProcessor, OrderingDbContext, OutboxOptions>(
        serviceScopeFactory,
        options,
        logger)
{
    protected override string ProcessorName => "Ordering";

    protected override async Task PublishAsync(
        IMessagePublisher publisher,
        IMessageNameResolver messageNameResolver,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        var reserveInventoryCommandType = messageNameResolver.ResolveMessageName(typeof(ReserveInventoryCommand));

        if (!string.Equals(message.MessageType, reserveInventoryCommandType, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported ordering outbox message type '{message.MessageType}'.");
        }

        var command = jsonHelper.DeserializeRequired<ReserveInventoryCommand>(message.Payload, $"Ordering outbox payload for message '{message.MessageId}' could not be deserialized.");

        var metadata = OutboxMessageMetadataFactory.Create(message);

        await publisher.PublishAsync(command, metadata, cancellationToken);
    }
}