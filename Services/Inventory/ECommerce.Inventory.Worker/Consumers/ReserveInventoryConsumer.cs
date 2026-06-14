using System.Text;
using System.Text.Json;
using ECommerce.Infrastructure.RabbitMq;
using ECommerce.Inventory.Application.ReserveInventory;
using ECommerce.Inventory.Worker.Options;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ECommerce.Inventory.Worker;

public sealed class ReserveInventoryConsumer(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RabbitMqSettings> rabbitMqOptions,
    IOptions<ReserveInventoryConsumerOptions> options,
    ILogger<ReserveInventoryConsumer> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqSettings _rabbitMq = rabbitMqOptions.Value;
    private readonly ReserveInventoryConsumerOptions _options = options.Value;

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

        channel.ExchangeDeclare(
            exchange: _rabbitMq.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        channel.QueueDeclare(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        channel.QueueBind(
            queue: _options.QueueName,
            exchange: _rabbitMq.ExchangeName,
            routingKey: _options.RoutingKey);

        channel.BasicQos(
            prefetchSize: 0,
            prefetchCount: _options.PrefetchCount,
            global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += ProcessMessageAsync;

        channel.BasicConsume(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer);

        logger.LogInformation(
            "Reserve inventory RabbitMQ consumer started. QueueName: {QueueName}, ExchangeName: {ExchangeName}, RoutingKey: {RoutingKey}",
            _options.QueueName,
            _rabbitMq.ExchangeName,
            _options.RoutingKey);

        return Task.CompletedTask;
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
            var command = JsonSerializer.Deserialize<ReserveInventoryCommand>(
                payload,
                SerializerOptions);

            if (command is null)
            {
                logger.LogWarning(
                    "Reserve inventory payload could not be deserialized. DeliveryTag: {DeliveryTag}",
                    args.DeliveryTag);

                channel.BasicReject(
                    deliveryTag: args.DeliveryTag,
                    requeue: false);

                return;
            }

            await using var scope = serviceScopeFactory.CreateAsyncScope();

            var handler = scope.ServiceProvider.GetRequiredService<ReserveInventoryCommandHandler>();

            await handler.HandleAsync(
                command,
                CreateMetadata(args.BasicProperties),
                payload,
                _stoppingToken);

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
                "Reserve inventory RabbitMQ consumer failed. DeliveryTag: {DeliveryTag}, MessageId: {MessageId}, CorrelationId: {CorrelationId}",
                args.DeliveryTag,
                args.BasicProperties.MessageId,
                args.BasicProperties.CorrelationId);

            channel.BasicNack(
                deliveryTag: args.DeliveryTag,
                multiple: false,
                requeue: false);
        }
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