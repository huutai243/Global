using ECommerce.Infrastructure.RabbitMq.Configuration;
using ECommerce.Infrastructure.RabbitMq.Consumers;
using ECommerce.Ordering.Application.InventoryReservation;
using ECommerce.Ordering.Worker.Options;
using ECommerce.Shared.Contracts.Inventory;
using ECommerce.Shared.Core.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;

namespace ECommerce.Ordering.Worker.Consumers;

public sealed class InventoryReservationResultConsumer(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RabbitMqSettings> rabbitMqOptions,
    IOptions<InventoryReservationResultConsumerOptions> options,
    IJsonHelper jsonHelper,
    ILogger<InventoryReservationResultConsumer> logger)
    : RabbitMqConsumerBase<InventoryReservationResultConsumer>(rabbitMqOptions.Value, logger)
{
    private readonly InventoryReservationResultConsumerOptions _options = options.Value;
    protected override string QueueName => _options.QueueName;
    protected override IReadOnlyCollection<string> RoutingKeys =>
    [
        _options.ReservedRoutingKey,
        _options.FailedRoutingKey
    ];
    protected override ushort PrefetchCount => _options.PrefetchCount;
    protected override int MaxRetryCount => _options.MaxRetryCount;
    protected override int RetryDelaySeconds => _options.RetryDelaySeconds;

    protected override async Task HandleMessageAsync(BasicDeliverEventArgs args, string payload, CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var handler = scope.ServiceProvider.GetRequiredService<InventoryReservationResultHandler>();
        var metadata = RabbitMqMessageMetadataFactory.Create(args.BasicProperties);

        if (string.Equals(args.RoutingKey, _options.ReservedRoutingKey, StringComparison.Ordinal))
        {
            var message = DeserializePayloadRequired<InventoryReservedEvent>(jsonHelper, payload, nameof(InventoryReservedEvent));

            await handler.HandleReservedAsync(message, metadata, payload, cancellationToken);

            return;
        }

        if (string.Equals(args.RoutingKey, _options.FailedRoutingKey, StringComparison.Ordinal))
        {
            var message = DeserializePayloadRequired<InventoryReservationFailedEvent>(
                jsonHelper,
                payload,
                nameof(InventoryReservationFailedEvent));

            await handler.HandleFailedAsync(message, metadata, payload, cancellationToken);

            return;
        }

        throw CreateInvalidMessageException($"Unsupported inventory reservation result routing key '{args.RoutingKey}'.");
    }
}