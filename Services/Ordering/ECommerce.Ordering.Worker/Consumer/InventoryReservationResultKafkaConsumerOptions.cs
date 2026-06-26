namespace ECommerce.Ordering.Worker.Options;

public sealed class InventoryReservationResultKafkaConsumerOptions
{
    public const string SectionName = "InventoryReservationResultKafkaConsumer";

    public string ReservedTopicName { get; init; } =
        "inventory.outbox.ECommerce.Shared.Contracts.InventoryReservedEvent";

    public string FailedTopicName { get; init; } =
        "inventory.outbox.ECommerce.Shared.Contracts.InventoryReservationFailedEvent";

    public string GroupId { get; init; } = "ordering.inventory-reservation-result";
}