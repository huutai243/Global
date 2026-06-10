namespace ECommerce.Inventory.Worker.Options;

public sealed class ReserveInventoryConsumerOptions
{
    public const string SectionName = "ReserveInventoryConsumer";

    public string QueueName { get; init; } = string.Empty;

    public int MaxConcurrentCalls { get; init; } = 4;
}