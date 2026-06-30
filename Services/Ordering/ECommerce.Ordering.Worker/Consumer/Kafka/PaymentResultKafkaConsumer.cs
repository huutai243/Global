using Confluent.Kafka;
using ECommerce.Infrastructure.Kafka.Configuration;
using ECommerce.Infrastructure.Kafka.Consumers;
using ECommerce.Ordering.Application.PaymentResult;
using ECommerce.Ordering.Worker.Options;
using ECommerce.Shared.Contracts.Payment;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Messaging;
using Microsoft.Extensions.Options;

namespace ECommerce.Ordering.Worker.Consumers.Kafka;

public sealed class PaymentResultKafkaConsumer(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<KafkaSettings> kafkaOptions,
    IOptions<PaymentResultKafkaConsumerOptions> options,
    IJsonHelper jsonHelper,
    IMessageNameResolver messageNameResolver,
    ILogger<PaymentResultKafkaConsumer> logger)
    : KafkaConsumerBase<PaymentResultKafkaConsumer>(kafkaOptions.Value, logger)
{
    private readonly PaymentResultKafkaConsumerOptions _options = options.Value;

    protected override IReadOnlyCollection<string> TopicNames =>
    [
        _options.SucceededTopicName,
        _options.FailedTopicName
    ];

    protected override string ConsumerGroupId => _options.GroupId;

    protected override async Task HandleMessageAsync(
        ConsumeResult<string, string> result,
        string payload,
        CancellationToken cancellationToken)
    {
        // EXACTLY-ONCE BUSINESS EFFECT NOTE:
        // Kafka/Debezium may redeliver payment results.
        // Ordering must rely on InboxMessage, unique constraints, and deterministic status transitions
        // before KafkaConsumerBase commits the offset.
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var handler = scope.ServiceProvider.GetRequiredService<PaymentResultHandler>();
        var metadata = KafkaMessageMetadataFactory.Create(result.Message.Headers);
        var messageType = GetMessageType(result);

        if (IsMessageType<PaymentSucceededEvent>(messageType))
        {
            var message = DeserializePayloadRequired<PaymentSucceededEvent>(
                jsonHelper,
                payload,
                nameof(PaymentSucceededEvent));

            await handler.HandleSucceededAsync(message, metadata, payload, cancellationToken);
            return;
        }

        if (IsMessageType<PaymentFailedEvent>(messageType))
        {
            var message = DeserializePayloadRequired<PaymentFailedEvent>(
                jsonHelper,
                payload,
                nameof(PaymentFailedEvent));

            await handler.HandleFailedAsync(message, metadata, payload, cancellationToken);
            return;
        }

        throw CreateInvalidMessageException(
            $"Unsupported payment result Kafka message type '{messageType}'.");
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
            var topic when string.Equals(topic, _options.SucceededTopicName, StringComparison.Ordinal)
                => messageNameResolver.ResolveMessageName(typeof(PaymentSucceededEvent)),

            var topic when string.Equals(topic, _options.FailedTopicName, StringComparison.Ordinal)
                => messageNameResolver.ResolveMessageName(typeof(PaymentFailedEvent)),

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