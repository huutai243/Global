namespace ECommerce.Core.SharedLibs.Interfaces;

public interface IKafkaProducer
{
    Task ProduceAsync<TMessage>(string topic, TMessage message, CancellationToken cancellationToken = default);
}
