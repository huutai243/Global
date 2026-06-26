namespace ECommerce.Infrastructure.Kafka.Configuration;

public sealed class KafkaSettings
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = "localhost:9092";
    public string ClientId { get; init; } = "ecommerce";
    public string GroupId { get; init; } = "ecommerce-worker";
    public string AutoOffsetReset { get; init; } = "Earliest";
    public bool EnableAutoCommit { get; init; } = false;
    public int ConsumeTimeoutMilliseconds { get; init; } = 1000;
}