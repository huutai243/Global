namespace ECommerce.Infrastructure.Persistence.Inbox;

// IDEMPOTENCY NOTE:
// InboxMessages are durable duplicate-detection records for at-least-once consumers.
// They help provide exactly-once business effect when paired with unique indexes and deterministic handlers.
//
// AUDIT NOTE:
// InboxMessages provide integration trace, but not a full business audit trail.
// A real audit trail should record actor, action, entity id, old value, new value, correlation id, and timestamp.
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