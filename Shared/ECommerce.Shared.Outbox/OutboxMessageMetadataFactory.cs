using ECommerce.Shared.Messaging;

namespace ECommerce.Shared.Outbox;

public static class OutboxMessageMetadataFactory
{
    public static MessageMetadata Create(OutboxMessage message)
    {
        return new MessageMetadata(
            message.MessageId,
            message.CorrelationId,
            message.CausationId,
            message.OccurredAtUtc);
    }
}