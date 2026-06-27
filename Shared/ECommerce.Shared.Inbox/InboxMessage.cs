namespace ECommerce.Shared.Inbox;

/// <summary>
/// Bản ghi Inbox dùng để chống xử lý trùng message ở consumer.
/// </summary>
/// <remarks>
/// Các message broker như RabbitMQ, Kafka hoặc Debezium/Kafka flow nên được xem là at-least-once delivery.
/// Nghĩa là cùng một message có thể được deliver nhiều hơn một lần.
///
/// InboxMessage lưu lại MessageId + ConsumerName để consumer biết message nào đã được nhận / xử lý / lỗi / dead-lettered.
/// Khi kết hợp với unique index và handler idempotent, Inbox giúp đạt exactly-once business effect.
///
/// Lưu ý:
/// InboxMessage là integration trace, không phải business audit trail đầy đủ.
/// Audit trail thật cần lưu actor, action, entity id, old value, new value, correlation id và timestamp.
/// </remarks>
public sealed class InboxMessage
{
    /// <summary>
    /// Khóa chính nội bộ của bản ghi Inbox.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Id ổn định của message, dùng để deduplicate.
    /// </summary>
    /// <remarks>
    /// Producer phải giữ MessageId ổn định cho cùng một business event.
    /// Nếu retry hoặc redelivery tạo MessageId mới, consumer sẽ khó chống xử lý trùng.
    /// </remarks>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Id dùng để liên kết toàn bộ message trong cùng một business flow.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Id của message hoặc command đã tạo ra message hiện tại.
    /// </summary>
    public string CausationId { get; set; } = string.Empty;

    /// <summary>
    /// Loại message được consumer xử lý.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Tên consumer xử lý message.
    /// </summary>
    /// <remarks>
    /// Cùng một MessageId có thể được nhiều consumer khác nhau xử lý.
    /// Vì vậy dedupe nên dựa trên cặp MessageId + ConsumerName, không chỉ MessageId.
    /// </remarks>
    public string ConsumerName { get; set; } = string.Empty;

    /// <summary>
    /// Payload gốc của message tại thời điểm consumer nhận được.
    /// </summary>
    /// <remarks>
    /// Payload giúp điều tra lỗi, replay thủ công hoặc debug khi consumer xử lý thất bại.
    /// Không nên xem Payload trong Inbox là audit log nghiệp vụ đầy đủ.
    /// </remarks>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái xử lý hiện tại của InboxMessage.
    /// </summary>
    public InboxMessageStatus Status { get; set; } = InboxMessageStatus.Received;

    /// <summary>
    /// Số lần consumer đã retry xử lý message này.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Số lần retry tối đa trước khi message được xem là dead-lettered.
    /// </summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>
    /// Lỗi gần nhất khi xử lý message thất bại.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Thời điểm consumer nhận message.
    /// </summary>
    public DateTime ReceivedAtUtc { get; set; }

    /// <summary>
    /// Thời điểm bắt đầu xử lý message.
    /// </summary>
    public DateTime? ProcessingStartedAtUtc { get; set; }

    /// <summary>
    /// Thời điểm xử lý message thành công.
    /// </summary>
    public DateTime? ProcessedAtUtc { get; set; }

    /// <summary>
    /// Thời điểm message được phép retry lần tiếp theo.
    /// </summary>
    public DateTime? NextRetryAtUtc { get; set; }

    /// <summary>
    /// Thời điểm message bị đưa vào trạng thái dead-lettered.
    /// </summary>
    /// <remarks>
    /// Message dead-lettered cần được monitor và có quy trình xử lý sau đó,
    /// ví dụ replay, quarantine hoặc reconciliation business state liên quan.
    /// </remarks>
    public DateTime? DeadLetteredAtUtc { get; set; }

    /// <summary>
    /// RowVersion dùng cho optimistic concurrency khi nhiều worker cùng thao tác trên một bản ghi Inbox.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}