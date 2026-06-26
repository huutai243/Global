using System.Text;
using Confluent.Kafka;
using SharedMessageMetadata = ECommerce.Shared.Messaging.MessageMetadata;

namespace ECommerce.Infrastructure.Kafka.Consumers;

public static class KafkaMessageMetadataFactory
{
    public static SharedMessageMetadata Create(Headers headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        var messageId = GetRequiredHeader(headers, "MessageId");
        var correlationId = GetOptionalHeader(headers, "CorrelationId") ?? messageId;
        var causationId = GetOptionalHeader(headers, "CausationId") ?? messageId;
        var occurredAtUtc = GetOccurredAtUtc(headers) ?? DateTime.UtcNow;

        return new SharedMessageMetadata(
            messageId,
            correlationId,
            causationId,
            occurredAtUtc);
    }

    public static string? GetOptionalHeader(Headers headers, string key)
    {
        var header = headers.LastOrDefault(header =>
            string.Equals(header.Key, key, StringComparison.OrdinalIgnoreCase));

        return header is null
            ? null
            : Encoding.UTF8.GetString(header.GetValueBytes());
    }

    private static string GetRequiredHeader(Headers headers, string key)
    {
        var value = GetOptionalHeader(headers, key);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidKafkaMessageException($"Kafka message is missing required header '{key}'.");
        }

        return value;
    }

    private static DateTime? GetOccurredAtUtc(Headers headers)
    {
        var value = GetOptionalHeader(headers, "OccurredAtUtc");

        return DateTime.TryParse(value, out var occurredAtUtc)
            ? occurredAtUtc
            : null;
    }

    public sealed class InvalidKafkaMessageException(string message) : Exception(message);
}