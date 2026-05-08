namespace ECommerce.Infrastructure.Kafka;

public sealed class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";

    public string DefaultTopic { get; set; } = "ecommerce-events";
}
