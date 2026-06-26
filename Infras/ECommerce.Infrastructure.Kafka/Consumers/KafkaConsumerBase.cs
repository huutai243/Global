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
    protected abstract string TopicName { get; }

    protected virtual string ConsumerGroupId => kafkaSettings.GroupId;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = CreateConsumer();

        consumer.Subscribe(TopicName);

        logger.LogInformation(
            "Kafka consumer started. Consumer: {Consumer}, TopicName: {TopicName}, GroupId: {GroupId}",
            typeof(TConsumer).Name,
            TopicName,
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
                consumer.Commit(result);
            }
        }
        catch (ConsumeException exception)
        {
            logger.LogError(
                exception,
                "Kafka consume failed. Consumer: {Consumer}, TopicName: {TopicName}",
                typeof(TConsumer).Name,
                TopicName);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Kafka message processing failed. Consumer: {Consumer}, Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, Key: {Key}",
                typeof(TConsumer).Name,
                result?.Topic,
                result?.Partition.Value,
                result?.Offset.Value,
                result?.Message.Key);

            // Không commit nếu business xử lý fail.
            // Message sẽ được đọc lại theo consumer group offset.
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
            EnableAutoCommit = kafkaSettings.EnableAutoCommit,
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