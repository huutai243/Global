using Confluent.Kafka;
using ECommerce.Infrastructure.Kafka.Configuration;
using ECommerce.Infrastructure.Kafka.Consumers;
using ECommerce.Inventory.Application.ReserveInventory;
using ECommerce.Inventory.Worker.Options;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Messaging;
using Microsoft.Extensions.Options;

namespace ECommerce.Inventory.Worker.Consumers.Kafka;

public sealed class ReserveInventoryKafkaConsumer(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<KafkaSettings> kafkaOptions,
    IOptions<ReserveInventoryKafkaConsumerOptions> options,
    IJsonHelper jsonHelper,
    IMessageNameResolver messageNameResolver,
    ILogger<ReserveInventoryKafkaConsumer> logger)
    : KafkaConsumerBase<ReserveInventoryKafkaConsumer>(kafkaOptions.Value, logger)
{
    private readonly ReserveInventoryKafkaConsumerOptions _options = options.Value;

    protected override string TopicName => _options.TopicName;

    protected override string ConsumerGroupId => _options.GroupId;

    protected override async Task HandleMessageAsync(
        ConsumeResult<string, string> result,
        string payload,
        CancellationToken cancellationToken)
    {
        ValidateMessageType(result);

        var command = DeserializePayloadRequired<ReserveInventoryCommand>(
            jsonHelper,
            payload,
            nameof(ReserveInventoryCommand));

        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var handler = scope.ServiceProvider.GetRequiredService<ReserveInventoryCommandHandler>();
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
            typeof(ReserveInventoryCommand));

        if (!string.Equals(actualMessageType, expectedMessageType, StringComparison.Ordinal))
        {
            throw CreateInvalidMessageException(
                $"Unsupported Kafka message type '{actualMessageType}'. Expected '{expectedMessageType}'.");
        }
    }
}