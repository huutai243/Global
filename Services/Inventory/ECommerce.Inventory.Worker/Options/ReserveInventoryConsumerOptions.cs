namespace ECommerce.Inventory.Worker.Options;

public sealed class ReserveInventoryConsumerOptions
{
    public const string SectionName = "ReserveInventoryConsumer";

    public string HostName { get; init; } = string.Empty;

    public int Port { get; init; } = 5672;

    public string UserName { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string ExchangeName { get; init; } = string.Empty;

    public string QueueName { get; init; } = string.Empty;

    public string RoutingKey { get; init; } = string.Empty;

    public ushort PrefetchCount { get; init; } = 4;
}