namespace ECommerce.Shared.Outbox;

// AUDIT NOTE:
// OutboxMessages provide integration trace, but not a full business audit trail.
// A real audit trail should record actor, action, entity id, old value, new value, correlation id, and timestamp.
//
// TODO RECONCILIATION:
// Monitor Pending/Failed/Processing/DeadLettered messages so committed business state is not stranded.
public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public string MessageId { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public string CausationId { get; set; } = string.Empty;

    public string MessageType { get; set; } = string.Empty;

    public string SourceService { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;

    public int RetryCount { get; set; }

    public int MaxRetryCount { get; set; } = 5;

    public string? ErrorMessage { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ProcessingStartedAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public DateTime? NextRetryAtUtc { get; set; }

    public DateTime? DeadLetteredAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
