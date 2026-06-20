namespace ECommerce.Ordering.Worker.Options;

public sealed class InventoryReservationResultConsumerOptions
{
    public const string SectionName = "InventoryReservationResultConsumer";

    public string QueueName { get; init; } = string.Empty;

    public string ReservedRoutingKey { get; init; } = string.Empty;

    public string FailedRoutingKey { get; init; } = string.Empty;

    public ushort PrefetchCount { get; init; } = 4;
}