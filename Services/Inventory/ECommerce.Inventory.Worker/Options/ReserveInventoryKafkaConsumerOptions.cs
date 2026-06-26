namespace ECommerce.Inventory.Worker.Options;

public sealed class ReserveInventoryKafkaConsumerOptions
{
    public const string SectionName = "ReserveInventoryKafkaConsumer";

    public string TopicName { get; init; } = "ordering.outbox.ECommerce.Shared.Contracts.ReserveInventoryCommand";

    public string GroupId { get; init; } = "inventory.reserve-inventory";
}