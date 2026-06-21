namespace ECommerce.Inventory.Worker.Options;

public sealed class ProductCreatedConsumerOptions
{
    public const string SectionName = "ProductCreatedConsumer";

    public string QueueName { get; init; } = string.Empty;

    public string RoutingKey { get; init; } = string.Empty;

    public ushort PrefetchCount { get; init; } = 4;

    public int MaxRetryCount { get; init; } = 5;

    public int RetryDelaySeconds { get; init; } = 10;
}