namespace ECommerce.Shared.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<TMessage>(TMessage message, MessageMetadata metadata, CancellationToken cancellationToken) where TMessage : class;
}