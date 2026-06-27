using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.RabbitMq.Consumers;

/// <summary>
/// Helper xử lý failure path cho RabbitMQ consumer: retry message hoặc chuyển sang dead-letter queue.
/// </summary>
/// <remarks>
/// RabbitMQ consumer dùng cơ chế at-least-once delivery.
/// Khi handler xử lý lỗi, message không được ack trực tiếp theo kiểu bỏ qua,
/// mà sẽ được publish lại sang retry exchange hoặc dead-letter exchange.
///
/// Lưu ý enterprise:
/// Retry / DLQ chỉ là cơ chế kỹ thuật để không mất message.
/// Hệ thống vẫn cần monitoring, alerting và operator tooling để xử lý poison message,
/// replay message hoặc reconciliation các business state bị lệch.
/// </remarks>
public static class RabbitMqConsumerFailureHandler
{
    private const string RetryCountHeader = "RetryCount";

    /// <summary>
    /// Retry message nếu chưa vượt quá số lần retry tối đa, ngược lại chuyển message sang dead-letter queue.
    /// </summary>
    /// <remarks>
    /// Method này publish message mới sang retry/dead-letter exchange rồi ack message gốc.
    /// Cách này tránh RabbitMQ deliver lại message gốc ngay lập tức,
    /// đồng thời cho phép retry queue kiểm soát delay thông qua TTL/topology.
    ///
    /// Rủi ro enterprise:
    /// Nếu publish sang retry/dead-letter exchange thành công nhưng ack message gốc thất bại,
    /// message có thể bị xử lý lại. Vì vậy consumer handler vẫn phải idempotent.
    /// </remarks>
    public static void RetryOrDeadLetter(
        IModel channel,
        BasicDeliverEventArgs args,
        string exchangeName,
        int maxRetryCount,
        ILogger logger,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(args);

        var retryCount = GetRetryCount(args.BasicProperties);

        if (retryCount >= maxRetryCount)
        {
            PublishToDeadLetter(
                channel,
                args,
                exchangeName,
                retryCount,
                logger,
                exception);

            channel.BasicAck(
                deliveryTag: args.DeliveryTag,
                multiple: false);

            return;
        }

        PublishToRetry(
            channel,
            args,
            exchangeName,
            retryCount + 1,
            logger,
            exception);

        channel.BasicAck(
            deliveryTag: args.DeliveryTag,
            multiple: false);
    }

    /// <summary>
    /// Chuyển invalid message trực tiếp sang dead-letter queue.
    /// </summary>
    /// <remarks>
    /// Invalid message thường là lỗi payload, schema, routing hoặc contract mismatch.
    /// Loại message này thường không thể tự hồi phục bằng retry,
    /// nên đưa thẳng vào DLQ để kiểm tra thủ công thay vì retry vô hạn.
    /// </remarks>
    public static void DeadLetterInvalidMessage(
        IModel channel,
        BasicDeliverEventArgs args,
        string exchangeName,
        ILogger logger,
        string reason)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(args);

        var retryCount = GetRetryCount(args.BasicProperties);

        var properties = CreateForwardProperties(
            channel,
            args.BasicProperties,
            retryCount);

        properties.Headers ??= new Dictionary<string, object>();
        properties.Headers["DeadLetterReason"] = reason;

        channel.BasicPublish(
            exchange: RabbitMqConsumerTopology.GetDeadLetterExchangeName(exchangeName),
            routingKey: args.RoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: args.Body);

        channel.BasicAck(
            deliveryTag: args.DeliveryTag,
            multiple: false);

        logger.LogWarning(
            "RabbitMQ message was moved to DLQ. RoutingKey: {RoutingKey}, MessageId: {MessageId}, Reason: {Reason}",
            args.RoutingKey,
            args.BasicProperties.MessageId,
            reason);
    }

    /// <summary>
    /// Publish message sang retry exchange với RetryCount mới.
    /// </summary>
    /// <remarks>
    /// RetryCount được lưu trong header để giới hạn số lần retry.
    /// Message sẽ đi qua retry queue và được đưa lại về main queue sau một khoảng delay
    /// do topology cấu hình.
    /// </remarks>
    private static void PublishToRetry(
        IModel channel,
        BasicDeliverEventArgs args,
        string exchangeName,
        int nextRetryCount,
        ILogger logger,
        Exception exception)
    {
        var properties = CreateForwardProperties(
            channel,
            args.BasicProperties,
            nextRetryCount);

        properties.Headers ??= new Dictionary<string, object>();
        properties.Headers["LastError"] = exception.Message;

        channel.BasicPublish(
            exchange: RabbitMqConsumerTopology.GetRetryExchangeName(exchangeName),
            routingKey: args.RoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: args.Body);

        logger.LogWarning(
            exception,
            "RabbitMQ message scheduled for retry. RoutingKey: {RoutingKey}, MessageId: {MessageId}, RetryCount: {RetryCount}",
            args.RoutingKey,
            args.BasicProperties.MessageId,
            nextRetryCount);
    }

    /// <summary>
    /// Publish message sang dead-letter exchange sau khi vượt quá số lần retry tối đa.
    /// </summary>
    /// <remarks>
    /// Dead-letter message không nên bị xem là đã xử lý xong về mặt business.
    /// Nó cần được monitor và có quy trình xử lý sau đó: replay, quarantine,
    /// cancel business flow hoặc escalate cho operator.
    /// </remarks>
    private static void PublishToDeadLetter(
        IModel channel,
        BasicDeliverEventArgs args,
        string exchangeName,
        int retryCount,
        ILogger logger,
        Exception exception)
    {
        var properties = CreateForwardProperties(
            channel,
            args.BasicProperties,
            retryCount);

        properties.Headers ??= new Dictionary<string, object>();
        properties.Headers["DeadLetterReason"] = exception.Message;

        channel.BasicPublish(
            exchange: RabbitMqConsumerTopology.GetDeadLetterExchangeName(exchangeName),
            routingKey: args.RoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: args.Body);

        logger.LogError(
            exception,
            "RabbitMQ message moved to DLQ after max retries. RoutingKey: {RoutingKey}, MessageId: {MessageId}, RetryCount: {RetryCount}",
            args.RoutingKey,
            args.BasicProperties.MessageId,
            retryCount);
    }

    /// <summary>
    /// Tạo properties mới khi forward message sang retry hoặc dead-letter exchange.
    /// </summary>
    /// <remarks>
    /// Các metadata quan trọng như MessageId, CorrelationId, Type và Headers được giữ lại
    /// để downstream logging, tracing, idempotency và troubleshooting vẫn hoạt động.
    ///
    /// RetryCount được cập nhật trong header để kiểm soát vòng đời retry của message.
    /// </remarks>
    private static IBasicProperties CreateForwardProperties(
        IModel channel,
        IBasicProperties source,
        int retryCount)
    {
        var properties = channel.CreateBasicProperties();

        properties.Persistent = true;
        properties.ContentType = source.ContentType ?? "application/json";
        properties.MessageId = source.MessageId;
        properties.CorrelationId = source.CorrelationId;
        properties.Type = source.Type;

        properties.Headers = source.Headers is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(source.Headers);

        properties.Headers[RetryCountHeader] = retryCount;

        return properties;
    }

    /// <summary>
    /// Đọc RetryCount từ message header.
    /// </summary>
    /// <remarks>
    /// RabbitMQ header có thể được deserialize thành nhiều kiểu dữ liệu khác nhau
    /// như int, long, byte[] hoặc string tùy client / producer.
    /// Nếu không đọc được RetryCount thì mặc định xem như lần xử lý đầu tiên.
    /// </remarks>
    private static int GetRetryCount(IBasicProperties properties)
    {
        if (properties.Headers is null ||
            !properties.Headers.TryGetValue(RetryCountHeader, out var value) ||
            value is null)
        {
            return 0;
        }

        return value switch
        {
            int number => number,
            long number => checked((int)number),
            byte[] bytes when int.TryParse(System.Text.Encoding.UTF8.GetString(bytes), out var number) => number,
            string text when int.TryParse(text, out var number) => number,
            _ => 0
        };
    }
}