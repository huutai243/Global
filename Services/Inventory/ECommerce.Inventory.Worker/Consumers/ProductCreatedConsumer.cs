using System.Text;
using System.Text.Json;
using ECommerce.Infrastructure.RabbitMq;
using ECommerce.Inventory.Application.ProductCreated;
using ECommerce.Inventory.Worker.Options;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ECommerce.Inventory.Worker.Consumers;

public sealed class ProductCreatedConsumer(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RabbitMqSettings> rabbitMqOptions,
    IOptions<ProductCreatedConsumerOptions> options,
    ILogger<ProductCreatedConsumer> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqSettings _rabbitMq = rabbitMqOptions.Value;
    private readonly ProductCreatedConsumerOptions _options = options.Value;

    private IConnection? _connection;
    private IModel? _channel;
    private CancellationToken _stoppingToken;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        var factory = new ConnectionFactory
        {
            HostName = _rabbitMq.HostName,
            Port = _rabbitMq.Port,
            UserName = _rabbitMq.UserName,
            Password = _rabbitMq.Password,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();

        var channel = _connection.CreateModel();
        _channel = channel;

        DeclareExchange(channel);
        DeclareQueue(channel);
        BindQueue(channel);
        ConfigureQos(channel);
        StartConsuming(channel);

        logger.LogInformation(
            "Product created RabbitMQ consumer started. QueueName: {QueueName}, ExchangeName: {ExchangeName}, RoutingKey: {RoutingKey}",
            _options.QueueName,
            _rabbitMq.ExchangeName,
            _options.RoutingKey);

        return Task.CompletedTask;
    }

    private void DeclareExchange(IModel channel)
    {
        channel.ExchangeDeclare(
            exchange: _rabbitMq.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);
    }

    private void DeclareQueue(IModel channel)
    {
        channel.QueueDeclare(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);
    }

    private void BindQueue(IModel channel)
    {
        channel.QueueBind(
            queue: _options.QueueName,
            exchange: _rabbitMq.ExchangeName,
            routingKey: _options.RoutingKey);
    }

    private void ConfigureQos(IModel channel)
    {
        channel.BasicQos(
            prefetchSize: 0,
            prefetchCount: _options.PrefetchCount,
            global: false);
    }

    private void StartConsuming(IModel channel)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += ProcessMessageAsync;

        channel.BasicConsume(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer);
    }

    private async Task ProcessMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        var channel = _channel;

        if (channel is null)
        {
            logger.LogError(
                "RabbitMQ channel is null. DeliveryTag: {DeliveryTag}",
                args.DeliveryTag);

            return;
        }

        var payload = Encoding.UTF8.GetString(args.Body.ToArray());

        try
        {
            var message = DeserializeMessage(payload, args);

            if (message is null)
            {
                channel.BasicReject(
                    deliveryTag: args.DeliveryTag,
                    requeue: false);

                return;
            }

            await HandleMessageAsync(message, args, payload);

            channel.BasicAck(
                deliveryTag: args.DeliveryTag,
                multiple: false);
        }
        catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
        {
            channel.BasicNack(
                deliveryTag: args.DeliveryTag,
                multiple: false,
                requeue: true);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "ProductCreatedEvent RabbitMQ consumer failed. DeliveryTag: {DeliveryTag}, MessageId: {MessageId}, CorrelationId: {CorrelationId}",
                args.DeliveryTag,
                args.BasicProperties.MessageId,
                args.BasicProperties.CorrelationId);

            channel.BasicNack(
                deliveryTag: args.DeliveryTag,
                multiple: false,
                requeue: false);
        }
    }

    private ProductCreatedEvent? DeserializeMessage(
        string payload,
        BasicDeliverEventArgs args)
    {
        var message = JsonSerializer.Deserialize<ProductCreatedEvent>(
            payload,
            SerializerOptions);

        if (message is null)
        {
            logger.LogWarning(
                "ProductCreatedEvent payload could not be deserialized. DeliveryTag: {DeliveryTag}",
                args.DeliveryTag);
        }

        return message;
    }

    private async Task HandleMessageAsync(
        ProductCreatedEvent message,
        BasicDeliverEventArgs args,
        string payload)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var handler = scope.ServiceProvider.GetRequiredService<ProductCreatedEventHandler>();

        await handler.HandleAsync(
            message,
            CreateMetadata(args.BasicProperties),
            payload,
            _stoppingToken);
    }

    private static MessageMetadata CreateMetadata(IBasicProperties properties)
    {
        var messageId = string.IsNullOrWhiteSpace(properties.MessageId)
            ? Guid.NewGuid().ToString("N")
            : properties.MessageId;

        var correlationId = string.IsNullOrWhiteSpace(properties.CorrelationId)
            ? messageId
            : properties.CorrelationId;

        var causationId = GetHeaderValue(properties, "CausationId") ?? messageId;

        return new MessageMetadata(
            messageId,
            correlationId,
            causationId,
            DateTime.UtcNow);
    }

    private static string? GetHeaderValue(IBasicProperties properties, string key)
    {
        if (properties.Headers is null ||
            !properties.Headers.TryGetValue(key, out var value) ||
            value is null)
        {
            return null;
        }

        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => value.ToString()
        };
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();

        _channel?.Dispose();
        _connection?.Dispose();

        return base.StopAsync(cancellationToken);
    }
}