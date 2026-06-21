using ECommerce.Infrastructure.RabbitMq.Configuration;
using ECommerce.Infrastructure.RabbitMq.Consumers;
using ECommerce.Inventory.Application.ProductCreated;
using ECommerce.Inventory.Worker.Options;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Core.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;

namespace ECommerce.Inventory.Worker.Consumers;

public sealed class ProductCreatedConsumer(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RabbitMqSettings> rabbitMqOptions,
    IOptions<ProductCreatedConsumerOptions> options,
    IJsonHelper jsonHelper,
    ILogger<ProductCreatedConsumer> logger)
    : RabbitMqConsumerBase<ProductCreatedConsumer>(rabbitMqOptions.Value, logger)
{
    private readonly ProductCreatedConsumerOptions _options = options.Value;

    protected override string QueueName => _options.QueueName;

    protected override IReadOnlyCollection<string> RoutingKeys => [_options.RoutingKey];

    protected override ushort PrefetchCount => _options.PrefetchCount;

    protected override int MaxRetryCount => _options.MaxRetryCount;

    protected override int RetryDelaySeconds => _options.RetryDelaySeconds;

    protected override async Task HandleMessageAsync(BasicDeliverEventArgs args, string payload, CancellationToken cancellationToken)
    {
        var message = DeserializePayloadRequired<ProductCreatedEvent>(jsonHelper, payload, nameof(ProductCreatedEvent));

        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var handler = scope.ServiceProvider.GetRequiredService<ProductCreatedEventHandler>();
        var metadata = RabbitMqMessageMetadataFactory.Create(args.BasicProperties);

        await handler.HandleAsync(message, metadata, payload, cancellationToken);
    }
}