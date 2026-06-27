using ECommerce.Infrastructure.RabbitMq.Configuration;
using ECommerce.Infrastructure.RabbitMq.Consumers;
using ECommerce.Inventory.Application.ReserveInventory;
using ECommerce.Inventory.Worker.Options;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Core.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;

namespace ECommerce.Inventory.Worker.Consumers;

public sealed class ReserveInventoryConsumer(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RabbitMqSettings> rabbitMqOptions,
    IOptions<ReserveInventoryConsumerOptions> options,
    IJsonHelper jsonHelper,
    ILogger<ReserveInventoryConsumer> logger)
    : RabbitMqConsumerBase<ReserveInventoryConsumer>(rabbitMqOptions.Value, logger)
{
    private readonly ReserveInventoryConsumerOptions _options = options.Value;

    protected override string QueueName => _options.QueueName;

    protected override IReadOnlyCollection<string> RoutingKeys => [_options.RoutingKey];

    protected override ushort PrefetchCount => _options.PrefetchCount;

    protected override int MaxRetryCount => _options.MaxRetryCount;

    protected override int RetryDelaySeconds => _options.RetryDelaySeconds;

    protected override async Task HandleMessageAsync(BasicDeliverEventArgs args, string payload, CancellationToken cancellationToken)
    {
        // EXACTLY-ONCE BUSINESS EFFECT NOTE:
        // This RabbitMQ reserve-inventory path is legacy/non-core while CDC/Kafka is active.
        // If enabled, it still requires the same idempotent handler, InboxMessage, and StockReservation uniqueness.
        var command = DeserializePayloadRequired<ReserveInventoryCommand>(jsonHelper, payload, nameof(ReserveInventoryCommand));

        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var handler = scope.ServiceProvider.GetRequiredService<ReserveInventoryCommandHandler>();
        var metadata = RabbitMqMessageMetadataFactory.Create(args.BasicProperties);

        await handler.HandleAsync(command, metadata, payload, cancellationToken);
    }
}
