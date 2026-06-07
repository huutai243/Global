using System.Text.Json;
using Azure.Messaging.ServiceBus;
using ECommerce.Shared.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.AzureServiceBus;

public sealed class AzureServiceBusMessagePublisher(
    ServiceBusClient serviceBusClient,
    IOptions<AzureServiceBusOptions> options,
    IMessageNameResolver messageNameResolver,
    ILogger<AzureServiceBusMessagePublisher> logger)
    : IMessagePublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly AzureServiceBusOptions _options = options.Value;

    public async Task PublishAsync<TMessage>(
        TMessage message,
        MessageMetadata metadata,
        CancellationToken cancellationToken)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(metadata);

        if (string.IsNullOrWhiteSpace(_options.TopicName))
        {
            throw new InvalidOperationException("Azure Service Bus topic name is not configured.");
        }

        var messageType = messageNameResolver.ResolveMessageName(typeof(TMessage));
        var messageBody = JsonSerializer.Serialize(message, SerializerOptions);

        await using var sender = serviceBusClient.CreateSender(_options.TopicName);

        var serviceBusMessage = new ServiceBusMessage(messageBody)
        {
            ContentType = "application/json",
            Subject = messageType,

            // Required for Inbox/idempotency because distributed brokers can deliver duplicated messages.
            MessageId = metadata.MessageId,

            // Required to trace one checkout saga across Ordering, Inventory and Payment.
            CorrelationId = metadata.CorrelationId
        };

        serviceBusMessage.ApplicationProperties["MessageType"] = messageType;
        serviceBusMessage.ApplicationProperties["CausationId"] = metadata.CausationId;
        serviceBusMessage.ApplicationProperties["OccurredAtUtc"] = metadata.OccurredAtUtc;

        await sender.SendMessageAsync(
            serviceBusMessage,
            cancellationToken);

        logger.LogInformation(
            "Published {MessageType} to Azure Service Bus. MessageId: {MessageId}, CorrelationId: {CorrelationId}, CausationId: {CausationId}",
            messageType,
            metadata.MessageId,
            metadata.CorrelationId,
            metadata.CausationId);
    }
}