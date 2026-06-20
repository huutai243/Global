using System.Text;
using System.Text.Json;
using ECommerce.Infrastructure.RabbitMq;
using ECommerce.Ordering.Application.InventoryReservation;
using ECommerce.Ordering.Worker.Options;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ECommerce.Ordering.Worker.Consumers;

public sealed class InventoryReservationResultConsumer(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RabbitMqSettings> rabbitMqOptions,
    IOptions<InventoryReservationResultConsumerOptions> options,
    ILogger<InventoryReservationResultConsumer> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqSettings _rabbitMq = rabbitMqOptions.Value;
    private readonly InventoryReservationResultConsumerOptions _options = options.Value;

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
            "Inventory reservation result RabbitMQ consumer started. QueueName: {QueueName}, ExchangeName: {ExchangeName}, ReservedRoutingKey: {ReservedRoutingKey}, FailedRoutingKey: {FailedRoutingKey}",
            _options.QueueName,
            _rabbitMq.ExchangeName,
            _options.ReservedRoutingKey,
            _options.FailedRoutingKey);

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
            routingKey: _options.ReservedRoutingKey);

        channel.QueueBind(
            queue: _options.QueueName,
            exchange: _rabbitMq.ExchangeName,
            routingKey: _options.FailedRoutingKey);
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
            await HandleMessageAsync(args, payload);

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
                "Inventory reservation result RabbitMQ consumer failed. DeliveryTag: {DeliveryTag}, RoutingKey: {RoutingKey}, MessageId: {MessageId}, CorrelationId: {CorrelationId}",
                args.DeliveryTag,
                args.RoutingKey,
                args.BasicProperties.MessageId,
                args.BasicProperties.CorrelationId);

            channel.BasicNack(
                deliveryTag: args.DeliveryTag,
                multiple: false,
                requeue: false);
        }
    }

    private async Task HandleMessageAsync(
        BasicDeliverEventArgs args,
        string payload)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var handler = scope.ServiceProvider.GetRequiredService<InventoryReservationResultHandler>();
        var metadata = CreateMetadata(args.BasicProperties);

        if (string.Equals(args.RoutingKey, _options.ReservedRoutingKey, StringComparison.Ordinal))
        {
            var message = DeserializeMessage<InventoryReservedEvent>(payload, args);

            await handler.HandleReservedAsync(
                message,
                metadata,
                payload,
                _stoppingToken);

            return;
        }

        if (string.Equals(args.RoutingKey, _options.FailedRoutingKey, StringComparison.Ordinal))
        {
            var message = DeserializeMessage<InventoryReservationFailedEvent>(payload, args);

            await handler.HandleFailedAsync(
                message,
                metadata,
                payload,
                _stoppingToken);

            return;
        }

        throw new InvalidOperationException(
            $"Unsupported inventory reservation result routing key '{args.RoutingKey}'.");
    }

    private TMessage DeserializeMessage<TMessage>(
        string payload,
        BasicDeliverEventArgs args)
        where TMessage : class
    {
        var message = JsonSerializer.Deserialize<TMessage>(
            payload,
            SerializerOptions);

        if (message is null)
        {
            throw new InvalidOperationException(
                $"Inventory reservation result payload could not be deserialized. DeliveryTag: {args.DeliveryTag}, RoutingKey: {args.RoutingKey}");
        }

        return message;
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