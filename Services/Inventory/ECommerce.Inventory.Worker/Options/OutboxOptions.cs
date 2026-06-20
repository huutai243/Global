namespace ECommerce.Inventory.Worker.Options;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int BatchSize { get; init; } = 20;

    public int PollingIntervalSeconds { get; init; } = 5;

    public int MaxRetryCount { get; init; } = 5;

    public int InitialRetryDelaySeconds { get; init; } = 5;

    public int MaxRetryDelaySeconds { get; init; } = 300;

    public int ProcessingTimeoutSeconds { get; init; } = 300;
}  