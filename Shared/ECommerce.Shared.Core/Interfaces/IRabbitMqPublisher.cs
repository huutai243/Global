namespace ECommerce.Shared.Core.Interfaces;

public interface IRabbitMqPublisher
{
    Task PublishAsync(string eventType, string payload, CancellationToken cancellationToken = default);
}
