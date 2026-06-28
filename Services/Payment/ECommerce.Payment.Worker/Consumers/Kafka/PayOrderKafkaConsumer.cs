
using Confluent.Kafka;
using ECommerce.Infrastructure.Kafka.Configuration;
using ECommerce.Infrastructure.Kafka.Consumers;
using ECommerce.Payment.Application.PayOrder;
using ECommerce.Payment.Worker.Options;
using ECommerce.Shared.Contracts.Payment;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Messaging;
using Microsoft.Extensions.Options;

namespace ECommerce.Payment.Worker.Consumers.Kafka;

public sealed class PayOrderKafkaConsumer(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<KafkaSettings> kafkaOptions,
    IOptions<PayOrderKafkaConsumerOptions> options,
    IJsonHelper jsonHelper,
    IMessageNameResolver messageNameResolver,
    ILogger<PayOrderKafkaConsumer> logger)
    : KafkaConsumerBase<PayOrderKafkaConsumer>(kafkaOptions.Value, logger)
{
    private readonly PayOrderKafkaConsumerOptions _options = options.Value;

    protected override IReadOnlyCollection<string> TopicNames => [_options.TopicName];

    protected override string ConsumerGroupId => _options.GroupId;

    protected override async Task HandleMessageAsync(
        ConsumeResult<string, string> result,
        string payload,
        CancellationToken cancellationToken)
    {
        // EXACTLY-ONCE BUSINESS EFFECT NOTE:
        // Kafka/Debezium should be treated as at-least-once delivery.
        // PayOrderCommandHandler must persist InboxMessage and enforce PaymentTransaction uniqueness
        // before KafkaConsumerBase commits the offset.
        ValidateMessageType(result);

        var command = DeserializePayloadRequired<PayOrderCommand>(
            jsonHelper,
            payload,
            nameof(PayOrderCommand));

        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var handler = scope.ServiceProvider.GetRequiredService<PayOrderCommandHandler>();
        var metadata = KafkaMessageMetadataFactory.Create(result.Message.Headers);

        await handler.HandleAsync(command, metadata, payload, cancellationToken);
    }

    private void ValidateMessageType(ConsumeResult<string, string> result)
    {
        var actualMessageType = KafkaMessageMetadataFactory.GetOptionalHeader(
            result.Message.Headers,
            "MessageType");

        if (string.IsNullOrWhiteSpace(actualMessageType))
        {
            return;
        }

        var expectedMessageType = messageNameResolver.ResolveMessageName(
            typeof(PayOrderCommand));

        if (!string.Equals(actualMessageType, expectedMessageType, StringComparison.Ordinal))
        {
            throw CreateInvalidMessageException(
                $"Unsupported Kafka message type '{actualMessageType}'. Expected '{expectedMessageType}'.");
        }
    }
}