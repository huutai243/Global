using System.Text;
using ECommerce.Shared.Messaging;
using RabbitMQ.Client;

namespace ECommerce.Infrastructure.RabbitMq.Consumers;

/// <summary>
/// Factory tạo MessageMetadata từ RabbitMQ message properties.
/// </summary>
/// <remarks>
/// MessageMetadata dùng để truyền các metadata quan trọng như MessageId, CorrelationId và CausationId
/// vào application handler.
///
/// Các metadata này giúp tracing, idempotency, debugging và liên kết các message trong cùng một business flow.
/// </remarks>
public static class RabbitMqMessageMetadataFactory
{
    /// <summary>
    /// Tạo MessageMetadata từ RabbitMQ BasicProperties.
    /// </summary>
    /// <remarks>
    /// MessageId là bắt buộc vì consumer cần nó để deduplicate bằng InboxMessage hoặc unique constraint.
    ///
    /// Nếu CorrelationId không có, MessageId được dùng làm fallback để vẫn giữ được trace chain.
    /// Nếu CausationId không có, MessageId được dùng làm fallback để xác định message hiện tại là nguyên nhân gần nhất.
    ///
    /// Lưu ý:
    /// RabbitMQ delivery là at-least-once, nên MessageId phải ổn định giữa các lần retry/re-delivery.
    /// Nếu producer tạo MessageId mới cho mỗi lần publish lại cùng một business event,
    /// consumer sẽ khó đạt exactly-once business effect.
    /// </remarks>
    public static MessageMetadata Create(IBasicProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        var messageId = string.IsNullOrWhiteSpace(properties.MessageId)
            ? throw new InvalidOperationException("RabbitMQ message is missing MessageId.")
            : properties.MessageId;

        var correlationId = string.IsNullOrWhiteSpace(properties.CorrelationId)
            ? messageId
            : properties.CorrelationId;

        var causationId = GetHeaderValue(properties, "CausationId") ?? messageId;

        return new MessageMetadata(
            messageId,
            correlationId,
            causationId,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Đọc giá trị header từ RabbitMQ BasicProperties.
    /// </summary>
    /// <remarks>
    /// RabbitMQ header có thể được deserialize thành nhiều kiểu khác nhau tùy client,
    /// phổ biến nhất là byte[] hoặc string.
    /// Method này chuẩn hóa header value về string để application layer sử dụng ổn định.
    /// </remarks>
    private static string? GetHeaderValue(IBasicProperties properties, string key)
    {
        if (properties.Headers is null ||
            !properties.Headers.TryGetValue(key, out var value) ||
            value is null)
        {
            return null;
        }

        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => value.ToString()
        };
    }
}