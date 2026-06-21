using System.Text;
using System.Text.Json;
using ECommerce.Infrastructure.RabbitMq.Configuration;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ECommerce.Infrastructure.RabbitMq.RabbitMqPublisher;

public sealed class RabbitMqPublisher(
    IOptions<RabbitMqSettings> options,
    IMessageNameResolver messageNameResolver,
    ILogger<RabbitMqPublisher> logger)
   : IRabbitMqPublisher, IMessagePublisher, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan PublisherConfirmTimeout = TimeSpan.FromSeconds(5);
    private readonly object _connectionLock = new();
    private IConnection? _connection;
    private bool _disposed;

    public Task PublishAsync(
        string eventType,
        string payload,
        CancellationToken cancellationToken = default)
    {
        PublishRaw(
            eventType,
            payload,
            messageId: null,
            correlationId: null,
            causationId: null,
            cancellationToken);

        return Task.CompletedTask;
    }

    public Task PublishAsync<TMessage>(
        TMessage message,
        MessageMetadata metadata,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(metadata);

        var routingKey = messageNameResolver.ResolveMessageName(typeof(TMessage));
        var payload = JsonSerializer.Serialize(message, SerializerOptions);

        PublishRaw(
            routingKey,
            payload,
            metadata.MessageId,
            metadata.CorrelationId,
            metadata.CausationId,
            cancellationToken);

        return Task.CompletedTask;
    }

    private void PublishRaw(
        string routingKey,
        string payload,
        string? messageId,
        string? correlationId,
        string? causationId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var settings = options.Value;
        var connection = GetOrCreateConnection(settings);

        using var channel = connection.CreateModel();

        DeclareExchange(channel, settings.ExchangeName);
        channel.ConfirmSelect();

        var returned = false;
        string? returnReason = null;

        void OnBasicReturn(object? sender, BasicReturnEventArgs args)
        {
            returned = true;
            returnReason = $"ReplyCode: {args.ReplyCode}, ReplyText: {args.ReplyText}";
        }

        channel.BasicReturn += OnBasicReturn;

        try
        {
            var properties = CreateProperties(
                channel,
                messageId,
                correlationId,
                causationId);

            var body = Encoding.UTF8.GetBytes(payload);

            channel.BasicPublish(
                exchange: settings.ExchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body);

            var confirmed = channel.WaitForConfirms(PublisherConfirmTimeout);

            if (!confirmed)
            {
                throw new InvalidOperationException(
                    $"RabbitMQ publish was not confirmed within {PublisherConfirmTimeout.TotalSeconds} seconds. RoutingKey: {routingKey}, MessageId: {messageId}");
            }

            if (returned)
            {
                throw new InvalidOperationException(
                    $"RabbitMQ message was returned as unroutable. RoutingKey: {routingKey}, MessageId: {messageId}, Reason: {returnReason}");
            }

            logger.LogDebug(
                "RabbitMQ publish confirmed. RoutingKey: {RoutingKey}, MessageId: {MessageId}, CorrelationId: {CorrelationId}",
                routingKey,
                messageId,
                correlationId);
        }
        finally
        {
            channel.BasicReturn -= OnBasicReturn;
        }
    }

    private IConnection GetOrCreateConnection(RabbitMqSettings settings)
    {
        if (_connection?.IsOpen == true)
        {
            return _connection;
        }

        lock (_connectionLock)
        {
            if (_connection?.IsOpen == true)
            {
                return _connection;
            }

            _connection?.Dispose();

            var factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true
            };

            _connection = factory.CreateConnection();

            logger.LogInformation(
                "RabbitMQ publisher connection created. HostName: {HostName}, Port: {Port}, ExchangeName: {ExchangeName}",
                settings.HostName,
                settings.Port,
                settings.ExchangeName);

            return _connection;
        }
    }

    private static void DeclareExchange(IModel channel, string exchangeName)
    {
        channel.ExchangeDeclare(
            exchange: exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);
    }

    private static IBasicProperties CreateProperties(
        IModel channel,
        string? messageId,
        string? correlationId,
        string? causationId)
    {
        var properties = channel.CreateBasicProperties();

        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = messageId;
        properties.CorrelationId = correlationId;
        properties.Headers = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(causationId))
        {
            properties.Headers["CausationId"] = causationId;
        }

        return properties;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _connection?.Close();
        }
        catch
        {
            // Ignore shutdown errors during dispose.
        }

        _connection?.Dispose();
    }
}