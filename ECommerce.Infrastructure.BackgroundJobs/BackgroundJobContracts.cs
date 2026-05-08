namespace ECommerce.Infrastructure.BackgroundJobs;

public interface IBackgroundJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

public interface IScheduledJob : IBackgroundJob
{
    string JobName { get; }

    TimeSpan Interval { get; }
}

public sealed class BackgroundJobSettings
{
    public bool Enabled { get; set; } = true;

    public int DefaultIntervalSeconds { get; set; } = 60;
}

public sealed class OutboxSettings
{
    public int BatchSize { get; set; } = 20;

    public int MaxRetryCount { get; set; } = 5;

    public int RetryDelaySeconds { get; set; } = 30;
}

public sealed class CleanupJobSettings
{
    public int IdempotencyRecordRetentionDays { get; set; } = 7;

    public int ProcessedOutboxRetentionDays { get; set; } = 7;

    public int AbandonedCartRetentionDays { get; set; } = 30;
}

public sealed class MonitoringJobSettings
{
    public int FailedOutboxWarningThreshold { get; set; } = 10;

    public int PendingPaymentWarningThreshold { get; set; } = 20;
}
