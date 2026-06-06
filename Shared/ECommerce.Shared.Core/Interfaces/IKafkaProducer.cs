namespace ECommerce.Shared.Core.Interfaces;

public interface IKafkaProducer
{
    Task ProduceAsync<TMessage>(string topic, TMessage message, CancellationToken cancellationToken = default);
}
