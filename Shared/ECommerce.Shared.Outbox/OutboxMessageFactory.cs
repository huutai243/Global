using ECommerce.Shared.Messaging;
using System.Text.Json;

namespace ECommerce.Shared.Outbox;

public sealed class OutboxMessageFactory(IMessageNameResolver messageNameResolver)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public OutboxMessage Create<TMessage>(
        TMessage message,
        string sourceService,
        string destination,
        string correlationId,
        string causationId,
        DateTime occurredAtUtc)
        where TMessage : class
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid().ToString("N"),
            CorrelationId = correlationId,
            CausationId = causationId,
            MessageType = messageNameResolver.ResolveMessageName(typeof(TMessage)),
            SourceService = sourceService,
            Destination = destination,
            Payload = JsonSerializer.Serialize(message, SerializerOptions),
            Status = OutboxMessageStatus.Pending,
            RetryCount = 0,
            OccurredAtUtc = occurredAtUtc,
            CreatedAtUtc = occurredAtUtc
        };
    }
}