using System.Text;
using ECommerce.Infrastructure.RabbitMq.Configuration;
using ECommerce.Shared.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ECommerce.Infrastructure.RabbitMq.Consumers;

public abstract class RabbitMqConsumerBase<TConsumer>(
    RabbitMqSettings rabbitMqSettings,
    ILogger<TConsumer> logger)
    : BackgroundService
{
    private IConnection? _connection;
    private IModel? _channel;
    private CancellationToken _stoppingToken;

    protected abstract string QueueName { get; }

    protected abstract IReadOnlyCollection<string> RoutingKeys { get; }

    protected abstract ushort PrefetchCount { get; }

    protected abstract int MaxRetryCount { get; }

    protected abstract int RetryDelaySeconds { get; }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        var factory = new ConnectionFactory
        {
            HostName = rabbitMqSettings.HostName,
            Port = rabbitMqSettings.Port,
            UserName = rabbitMqSettings.UserName,
            Password = rabbitMqSettings.Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };

        _connection = factory.CreateConnection();

        var channel = _connection.CreateModel();
        _channel = channel;

        RabbitMqConsumerTopology.DeclareConsumerTopology(
            channel,
            rabbitMqSettings.ExchangeName,
            QueueName,
            RoutingKeys,
            RetryDelaySeconds);

        ConfigureQos(channel);
        StartConsuming(channel);

        logger.LogInformation(
            "RabbitMQ consumer started. Consumer: {Consumer}, QueueName: {QueueName}, ExchangeName: {ExchangeName}, RoutingKeys: {RoutingKeys}, MaxRetryCount: {MaxRetryCount}, RetryDelaySeconds: {RetryDelaySeconds}",
            typeof(TConsumer).Name,
            QueueName,
            rabbitMqSettings.ExchangeName,
            string.Join(", ", RoutingKeys),
            MaxRetryCount,
            RetryDelaySeconds);

        return Task.CompletedTask;
    }

    private void ConfigureQos(IModel channel)
    {
        channel.BasicQos(
            prefetchSize: 0,
            prefetchCount: PrefetchCount,
            global: false);
    }

    private void StartConsuming(IModel channel)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += ProcessMessageAsync;

        channel.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);
    }

    private async Task ProcessMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        var channel = _channel;

        if (channel is null)
        {
            logger.LogError(
                "RabbitMQ channel is null. Consumer: {Consumer}, DeliveryTag: {DeliveryTag}",
                typeof(TConsumer).Name,
                args.DeliveryTag);

            return;
        }

        var payload = Encoding.UTF8.GetString(args.Body.ToArray());

        try
        {
            await HandleMessageAsync(args, payload, _stoppingToken);

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
        catch (InvalidRabbitMqMessageException exception)
        {
            logger.LogWarning(
                exception,
                "RabbitMQ invalid message moved to DLQ. Consumer: {Consumer}, DeliveryTag: {DeliveryTag}, RoutingKey: {RoutingKey}, MessageId: {MessageId}, CorrelationId: {CorrelationId}",
                typeof(TConsumer).Name,
                args.DeliveryTag,
                args.RoutingKey,
                args.BasicProperties.MessageId,
                args.BasicProperties.CorrelationId);

            RabbitMqConsumerFailureHandler.DeadLetterInvalidMessage(
                channel,
                args,
                rabbitMqSettings.ExchangeName,
                logger,
                exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "RabbitMQ consumer failed. Consumer: {Consumer}, DeliveryTag: {DeliveryTag}, RoutingKey: {RoutingKey}, MessageId: {MessageId}, CorrelationId: {CorrelationId}",
                typeof(TConsumer).Name,
                args.DeliveryTag,
                args.RoutingKey,
                args.BasicProperties.MessageId,
                args.BasicProperties.CorrelationId);

            RabbitMqConsumerFailureHandler.RetryOrDeadLetter(
                channel,
                args,
                rabbitMqSettings.ExchangeName,
                MaxRetryCount,
                logger,
                exception);
        }
    }

    protected abstract Task HandleMessageAsync(
        BasicDeliverEventArgs args,
        string payload,
        CancellationToken cancellationToken);

    protected static TMessage DeserializePayloadRequired<TMessage>(
        IJsonHelper jsonHelper,
        string payload,
        string messageName)
        where TMessage : class
    {
        try
        {
            return jsonHelper.DeserializeRequired<TMessage>(
                payload,
                $"{messageName} payload could not be deserialized.");
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidRabbitMqMessageException(exception.Message);
        }
    }

    protected static InvalidRabbitMqMessageException CreateInvalidMessageException(string message)
    {
        return new InvalidRabbitMqMessageException(message);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();

        _channel?.Dispose();
        _connection?.Dispose();

        return base.StopAsync(cancellationToken);
    }

    public sealed class InvalidRabbitMqMessageException(string message) : Exception(message);
}