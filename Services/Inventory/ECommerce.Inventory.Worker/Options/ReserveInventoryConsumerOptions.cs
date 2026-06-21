namespace ECommerce.Inventory.Worker.Options;

public sealed class ReserveInventoryConsumerOptions
{
    public const string SectionName = "ReserveInventoryConsumer";

    public string QueueName { get; init; } = "inventory.reserve-inventory";

    public string RoutingKey { get; init; } = "ECommerce.Shared.Contracts.ReserveInventoryCommand";

    public ushort PrefetchCount { get; init; } = 4;

    public int MaxRetryCount { get; init; } = 5;

    public int RetryDelaySeconds { get; init; } = 10;
}