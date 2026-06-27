using Confluent.Kafka;
using ECommerce.Infrastructure.Kafka.Configuration;
using ECommerce.Shared.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static ECommerce.Infrastructure.Kafka.Consumers.KafkaMessageMetadataFactory;

namespace ECommerce.Infrastructure.Kafka.Consumers;

public abstract class KafkaConsumerBase<TConsumer>(
    KafkaSettings kafkaSettings,
    ILogger<TConsumer> logger)
    : BackgroundService
    where TConsumer : class
{
    protected abstract IReadOnlyCollection<string> TopicNames { get; }

    protected virtual string ConsumerGroupId => kafkaSettings.GroupId;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = CreateConsumer();

        consumer.Subscribe(TopicNames);

        logger.LogInformation(
            "Kafka consumer started. Consumer: {Consumer}, Topics: {Topics}, GroupId: {GroupId}",
            typeof(TConsumer).Name,
            string.Join(", ", TopicNames),
            ConsumerGroupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ConsumeNextAsync(consumer, stoppingToken);
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ConsumeNextAsync(
        IConsumer<string, string> consumer,
        CancellationToken cancellationToken)
    {
        ConsumeResult<string, string>? result = null;

        try
        {
            result = consumer.Consume(
                TimeSpan.FromMilliseconds(kafkaSettings.ConsumeTimeoutMilliseconds));

            if (result?.Message is null)
            {
                return;
            }

            await HandleMessageAsync(
                result,
                result.Message.Value,
                cancellationToken);

            // EXACTLY-ONCE BUSINESS EFFECT NOTE:
            // This manual commit happens only after the handler returns successfully.
            // Kafka delivery is still at-least-once; idempotent handlers, InboxMessage rows,
            // unique constraints, and deterministic state transitions provide the business guarantee.
            consumer.Commit(result);

            logger.LogInformation(
                "Kafka message processed. Consumer: {Consumer}, Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, Key: {Key}",
                typeof(TConsumer).Name,
                result.Topic,
                result.Partition.Value,
                result.Offset.Value,
                result.Message.Key);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidKafkaMessageException exception)
        {
            logger.LogWarning(
                exception,
                "Kafka invalid message skipped. Consumer: {Consumer}, Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, Key: {Key}",
                typeof(TConsumer).Name,
                result?.Topic,
                result?.Partition.Value,
                result?.Offset.Value,
                result?.Message.Key);

            if (result is not null)
            {
                // EXACTLY-ONCE BUSINESS EFFECT NOTE:
                // Invalid messages are intentionally committed/skipped here.
                // Add a Kafka DLQ or quarantine topic if operators need later replay/inspection.
                consumer.Commit(result);
            }
        }
        catch (ConsumeException exception)
        {
            logger.LogError(
                exception,
                "Kafka consume failed. Consumer: {Consumer}, Topics: {Topics}",
                typeof(TConsumer).Name,
                string.Join(", ", TopicNames));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Kafka message processing failed. Consumer: {Consumer}, Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, Key: {Key}. Consumer will stop to avoid committing past failed offset.",
                typeof(TConsumer).Name,
                result?.Topic,
                result?.Partition.Value,
                result?.Offset.Value,
                result?.Message.Key);

            throw;
        }
    }

    protected abstract Task HandleMessageAsync(
        ConsumeResult<string, string> result,
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
            throw new InvalidKafkaMessageException(exception.Message);
        }
    }

    protected static InvalidKafkaMessageException CreateInvalidMessageException(string message)
    {
        return new InvalidKafkaMessageException(message);
    }

    private IConsumer<string, string> CreateConsumer()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaSettings.BootstrapServers,
            ClientId = kafkaSettings.ClientId,
            GroupId = ConsumerGroupId,
            EnableAutoCommit = false,
            AutoOffsetReset = ParseAutoOffsetReset(kafkaSettings.AutoOffsetReset),
            EnablePartitionEof = false
        };

        return new ConsumerBuilder<string, string>(config).Build();
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string value)
    {
        return string.Equals(value, "Latest", StringComparison.OrdinalIgnoreCase)
            ? AutoOffsetReset.Latest
            : AutoOffsetReset.Earliest;
    }
}
