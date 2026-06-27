using Confluent.Kafka;
using ECommerce.Infrastructure.Kafka.Configuration;
using ECommerce.Infrastructure.Kafka.Consumers;
using ECommerce.Ordering.Application.InventoryReservation;
using ECommerce.Ordering.Worker.Options;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Messaging;
using Microsoft.Extensions.Options;

namespace ECommerce.Ordering.Worker.Consumers.Kafka;

public sealed class InventoryReservationResultKafkaConsumer(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<KafkaSettings> kafkaOptions,
    IOptions<InventoryReservationResultKafkaConsumerOptions> options,
    IJsonHelper jsonHelper,
    IMessageNameResolver messageNameResolver,
    ILogger<InventoryReservationResultKafkaConsumer> logger)
    : KafkaConsumerBase<InventoryReservationResultKafkaConsumer>(kafkaOptions.Value, logger)
{
    private readonly InventoryReservationResultKafkaConsumerOptions _options = options.Value;

    protected override IReadOnlyCollection<string> TopicNames =>
    [
        _options.ReservedTopicName,
        _options.FailedTopicName
    ];

    protected override string ConsumerGroupId => _options.GroupId;

    protected override async Task HandleMessageAsync(
        ConsumeResult<string, string> result,
        string payload,
        CancellationToken cancellationToken)
    {
        // EXACTLY-ONCE BUSINESS EFFECT NOTE:
        // Kafka/Debezium may redeliver reservation results.
        // Ordering must rely on InboxMessage, unique constraints, and deterministic status transitions before committing offsets.
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var handler = scope.ServiceProvider.GetRequiredService<InventoryReservationResultHandler>();
        var metadata = KafkaMessageMetadataFactory.Create(result.Message.Headers);
        var messageType = GetMessageType(result);

        if (IsMessageType<InventoryReservedEvent>(messageType))
        {
            var message = DeserializePayloadRequired<InventoryReservedEvent>(
                jsonHelper,
                payload,
                nameof(InventoryReservedEvent));

            await handler.HandleReservedAsync(message, metadata, payload, cancellationToken);
            return;
        }

        if (IsMessageType<InventoryReservationFailedEvent>(messageType))
        {
            var message = DeserializePayloadRequired<InventoryReservationFailedEvent>(
                jsonHelper,
                payload,
                nameof(InventoryReservationFailedEvent));

            await handler.HandleFailedAsync(message, metadata, payload, cancellationToken);
            return;
        }

        throw CreateInvalidMessageException(
            $"Unsupported inventory reservation result Kafka message type '{messageType}'.");
    }

    private string GetMessageType(ConsumeResult<string, string> result)
    {
        var messageType = KafkaMessageMetadataFactory.GetOptionalHeader(
            result.Message.Headers,
            "MessageType");

        if (!string.IsNullOrWhiteSpace(messageType))
        {
            return messageType;
        }

        return result.Topic switch
        {
            var topic when string.Equals(topic, _options.ReservedTopicName, StringComparison.Ordinal)
                => messageNameResolver.ResolveMessageName(typeof(InventoryReservedEvent)),

            var topic when string.Equals(topic, _options.FailedTopicName, StringComparison.Ordinal)
                => messageNameResolver.ResolveMessageName(typeof(InventoryReservationFailedEvent)),

            _ => throw CreateInvalidMessageException(
                $"Kafka message is missing MessageType header. Topic: {result.Topic}")
        };
    }

    private bool IsMessageType<TMessage>(string actualMessageType)
    {
        var expectedMessageType = messageNameResolver.ResolveMessageName(typeof(TMessage));

        return string.Equals(actualMessageType, expectedMessageType, StringComparison.Ordinal);
    }
}
