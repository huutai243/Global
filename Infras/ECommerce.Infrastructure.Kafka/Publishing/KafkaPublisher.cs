using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using ECommerce.Infrastructure.Kafka.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedMessageMetadata = ECommerce.Shared.Messaging.MessageMetadata;

namespace ECommerce.Infrastructure.Kafka.Publishing;

public interface IKafkaPublisher
{
    Task PublishAsync<TMessage>(
        string topic,
        string key,
        TMessage message,
        SharedMessageMetadata metadata,
        CancellationToken cancellationToken = default)
        where TMessage : class;
}

public sealed class KafkaPublisher(
    IOptions<KafkaSettings> options,
    ILogger<KafkaPublisher> logger)
    : IKafkaPublisher, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IProducer<string, string> _producer = CreateProducer(options.Value);
    private bool _disposed;

    public async Task PublishAsync<TMessage>(
        string topic,
        string key,
        TMessage message,
        SharedMessageMetadata metadata,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(metadata);

        ThrowIfDisposed();

        var payload = JsonSerializer.Serialize(message, SerializerOptions);

        var kafkaMessage = new Message<string, string>
        {
            Key = key,
            Value = payload,
            Headers = CreateHeaders(metadata)
        };

        var result = await _producer.ProduceAsync(
            topic,
            kafkaMessage,
            cancellationToken);

        logger.LogInformation(
            "Kafka message published. Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, Key: {Key}, MessageId: {MessageId}, CorrelationId: {CorrelationId}",
            result.Topic,
            result.Partition.Value,
            result.Offset.Value,
            key,
            metadata.MessageId,
            metadata.CorrelationId);
    }

    private static IProducer<string, string> CreateProducer(KafkaSettings settings)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            ClientId = settings.ClientId,
            EnableIdempotence = true,
            Acks = Acks.All
        };

        return new ProducerBuilder<string, string>(config).Build();
    }

    private static Headers CreateHeaders(SharedMessageMetadata metadata)
    {
        var headers = new Headers();

        AddHeader(headers, "MessageId", metadata.MessageId);
        AddHeader(headers, "CorrelationId", metadata.CorrelationId);
        AddHeader(headers, "CausationId", metadata.CausationId);
        AddHeader(headers, "OccurredAtUtc", metadata.OccurredAtUtc.ToString("O"));

        return headers;
    }

    private static void AddHeader(Headers headers, string key, string value)
    {
        headers.Add(key, Encoding.UTF8.GetBytes(value));
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

        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}