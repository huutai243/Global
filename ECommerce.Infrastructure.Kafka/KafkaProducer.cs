using System.Text.Json;
using Confluent.Kafka;
using ECommerce.Core.SharedLibs.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Kafka;

public sealed class KafkaProducer(IOptions<KafkaSettings> options, ILogger<KafkaProducer> logger) : IKafkaProducer
{
    public async Task ProduceAsync<TMessage>(string topic, TMessage message, CancellationToken cancellationToken = default)
    {
        var config = new ProducerConfig { BootstrapServers = options.Value.BootstrapServers };
        using var producer = new ProducerBuilder<Null, string>(config).Build();
        await producer.ProduceAsync(
            topic,
            new Message<Null, string> { Value = JsonSerializer.Serialize(message) },
            cancellationToken);
        logger.LogInformation("Kafka skeleton produced message to topic {Topic}", topic);
    }
}
