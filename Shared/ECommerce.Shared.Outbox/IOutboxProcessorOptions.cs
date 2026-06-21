namespace ECommerce.Shared.Outbox;

public interface IOutboxProcessorOptions
{
    int BatchSize { get; }

    int PollingIntervalSeconds { get; }

    int MaxRetryCount { get; }

    int InitialRetryDelaySeconds { get; }

    int MaxRetryDelaySeconds { get; }

    int ProcessingTimeoutSeconds { get; }
}