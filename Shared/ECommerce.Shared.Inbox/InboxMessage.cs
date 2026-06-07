namespace ECommerce.Shared.Inbox;

public sealed class InboxMessage
{
    public Guid Id { get; set; }

    public string MessageId { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public string CausationId { get; set; } = string.Empty;

    public string MessageType { get; set; } = string.Empty;

    public string ConsumerName { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public InboxMessageStatus Status { get; set; } = InboxMessageStatus.Received;

    public int RetryCount { get; set; }

    public int MaxRetryCount { get; set; } = 5;

    public string? ErrorMessage { get; set; }

    public DateTime ReceivedAtUtc { get; set; }

    public DateTime? ProcessingStartedAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public DateTime? NextRetryAtUtc { get; set; }

    public DateTime? DeadLetteredAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
