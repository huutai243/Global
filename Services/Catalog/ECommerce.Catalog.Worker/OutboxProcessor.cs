using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Catalog.Worker.Options;
using ECommerce.Infrastructure.BackgroundJobs;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Messaging;
using ECommerce.Shared.Outbox;
using Microsoft.Extensions.Options;

namespace ECommerce.Catalog.Worker;

public sealed class OutboxProcessor(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<OutboxOptions> options,
    IJsonHelper jsonHelper,
    ILogger<OutboxProcessor> logger)
    : OutboxProcessorBase<OutboxProcessor, CatalogDbContext, OutboxOptions>(
        serviceScopeFactory,
        options,
        logger)
{
    protected override string ProcessorName => "Catalog";

    protected override async Task PublishAsync(
        IMessagePublisher publisher,
        IMessageNameResolver messageNameResolver,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        var productCreatedEventType = messageNameResolver.ResolveMessageName(typeof(ProductCreatedEvent));

        if (!string.Equals(message.MessageType, productCreatedEventType, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported catalog outbox message type '{message.MessageType}'.");
        }

        var @event = jsonHelper.DeserializeRequired<ProductCreatedEvent>(message.Payload, $"Catalog outbox payload for message '{message.MessageId}' could not be deserialized.");
        var metadata = OutboxMessageMetadataFactory.Create(message);

        await publisher.PublishAsync(@event, metadata, cancellationToken);
    }
}