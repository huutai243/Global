using System.Text;
using System.Text.Json;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ECommerce.Infrastructure.RabbitMq;

public sealed class RabbitMqPublisher(
    IOptions<RabbitMqSettings> options,
    IMessageNameResolver messageNameResolver,
    ILogger<RabbitMqPublisher> logger)
    : IRabbitMqPublisher, IMessagePublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public Task PublishAsync(string eventType, string payload, CancellationToken cancellationToken = default)
    {
        PublishRaw(eventType, payload, messageId: null, correlationId: null, causationId: null);
        return Task.CompletedTask;
    }

    public Task PublishAsync<TMessage>(
        TMessage message,
        MessageMetadata metadata,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var routingKey = messageNameResolver.ResolveMessageName(typeof(TMessage));
        var payload = JsonSerializer.Serialize(message, SerializerOptions);

        PublishRaw(
            routingKey,
            payload,
            metadata.MessageId,
            metadata.CorrelationId,
            metadata.CausationId);

        return Task.CompletedTask;
    }

    private void PublishRaw(
        string routingKey,
        string payload,
        string? messageId,
        string? correlationId,
        string? causationId)
    {
        var settings = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(
            exchange: settings.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = messageId;
        properties.CorrelationId = correlationId;
        properties.Headers = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(causationId))
        {
            properties.Headers["CausationId"] = causationId;
        }

        var body = Encoding.UTF8.GetBytes(payload);

        channel.BasicPublish(
            exchange: settings.ExchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        logger.LogInformation(
            "RabbitMQ publish succeeded. RoutingKey: {RoutingKey}, MessageId: {MessageId}, CorrelationId: {CorrelationId}",
            routingKey,
            messageId,
            correlationId);
    }
}