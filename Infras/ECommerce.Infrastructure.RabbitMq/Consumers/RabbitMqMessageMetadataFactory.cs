using System.Text;
using ECommerce.Shared.Messaging;
using RabbitMQ.Client;

namespace ECommerce.Infrastructure.RabbitMq.Consumers;

public static class RabbitMqMessageMetadataFactory
{
    public static MessageMetadata Create(IBasicProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        var messageId = string.IsNullOrWhiteSpace(properties.MessageId)
            ? throw new InvalidOperationException("RabbitMQ message is missing MessageId.")
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
}