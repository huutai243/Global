using System.Text;
using System.Text.RegularExpressions;
using RabbitMQ.Client;

namespace ECommerce.Infrastructure.RabbitMq.Consumers;

/// <summary>
/// Helper khai báo RabbitMQ topology cho consumer: main exchange, retry exchange, dead-letter exchange,
/// main queue, retry queue và dead-letter queue.
/// </summary>
/// <remarks>
/// Topology này giúp consumer xử lý message theo hướng at-least-once delivery:
/// message lỗi sẽ được chuyển sang retry queue có TTL, sau đó quay lại main queue để xử lý lại.
/// Nếu vượt quá số lần retry, message sẽ được đưa vào dead-letter queue.
///
/// Đây là cơ chế hạ tầng để tránh mất message, nhưng không thay thế cho idempotency,
/// monitoring, replay tooling hoặc reconciliation ở tầng business.
/// </remarks>
public static class RabbitMqConsumerTopology
{
    private const string RetryExchangeSuffix = ".retry";
    private const string DeadLetterExchangeSuffix = ".dead";

    /// <summary>
    /// Khai báo toàn bộ topology cần thiết cho một RabbitMQ consumer.
    /// </summary>
    /// <remarks>
    /// Method này đảm bảo các exchange và queue cần thiết tồn tại trước khi consumer bắt đầu consume.
    /// Mỗi routing key sẽ được bind vào main queue, retry queue và dead-letter queue tương ứng.
    ///
    /// Retry queue dùng TTL và dead-letter routing để đưa message quay lại main exchange sau một khoảng delay.
    /// Dead-letter queue giữ lại message không thể xử lý tự động để phục vụ điều tra, replay hoặc xử lý thủ công.
    /// </remarks>
    public static void DeclareConsumerTopology(
        IModel channel,
        string exchangeName,
        string queueName,
        IReadOnlyCollection<string> routingKeys,
        int retryDelaySeconds)
    {
        ArgumentNullException.ThrowIfNull(channel);

        if (string.IsNullOrWhiteSpace(exchangeName))
        {
            throw new InvalidOperationException("RabbitMQ exchange name is required.");
        }

        if (string.IsNullOrWhiteSpace(queueName))
        {
            throw new InvalidOperationException("RabbitMQ queue name is required.");
        }

        if (routingKeys.Count == 0)
        {
            throw new InvalidOperationException("At least one RabbitMQ routing key is required.");
        }

        var retryExchangeName = GetRetryExchangeName(exchangeName);
        var deadLetterExchangeName = GetDeadLetterExchangeName(exchangeName);
        var retryDelayMilliseconds = retryDelaySeconds * 1000;

        channel.ExchangeDeclare(
            exchange: exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        channel.ExchangeDeclare(
            exchange: retryExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        channel.ExchangeDeclare(
            exchange: deadLetterExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        foreach (var routingKey in routingKeys.Distinct(StringComparer.Ordinal))
        {
            channel.QueueBind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: routingKey);

            DeclareRetryQueue(
                channel,
                exchangeName,
                retryExchangeName,
                queueName,
                routingKey,
                retryDelayMilliseconds);

            DeclareDeadLetterQueue(
                channel,
                deadLetterExchangeName,
                queueName,
                routingKey);
        }
    }

    /// <summary>
    /// Tạo tên retry exchange từ tên main exchange.
    /// </summary>
    public static string GetRetryExchangeName(string exchangeName)
    {
        return $"{exchangeName}{RetryExchangeSuffix}";
    }

    /// <summary>
    /// Tạo tên dead-letter exchange từ tên main exchange.
    /// </summary>
    public static string GetDeadLetterExchangeName(string exchangeName)
    {
        return $"{exchangeName}{DeadLetterExchangeSuffix}";
    }

    /// <summary>
    /// Tạo tên retry queue theo queue chính và routing key.
    /// </summary>
    /// <remarks>
    /// Retry queue được tách theo routing key để message lỗi có thể retry đúng route ban đầu.
    /// Routing key được chuyển thành suffix an toàn để tránh tên queue quá dài hoặc chứa ký tự không phù hợp.
    /// </remarks>
    public static string GetRetryQueueName(string queueName, string routingKey)
    {
        return $"{queueName}.retry.{CreateSafeRoutingKeySuffix(routingKey)}";
    }

    /// <summary>
    /// Tạo tên dead-letter queue từ tên queue chính.
    /// </summary>
    public static string GetDeadLetterQueueName(string queueName)
    {
        return $"{queueName}.dead";
    }

    /// <summary>
    /// Khai báo retry queue cho một routing key.
    /// </summary>
    /// <remarks>
    /// Retry queue dùng x-message-ttl để giữ message trong một khoảng delay.
    /// Sau khi TTL hết hạn, RabbitMQ sẽ dead-letter message về main exchange với routing key ban đầu.
    /// Nhờ vậy message được retry có kiểm soát thay vì bị requeue ngay lập tức.
    /// </remarks>
    private static void DeclareRetryQueue(
        IModel channel,
        string mainExchangeName,
        string retryExchangeName,
        string queueName,
        string routingKey,
        int retryDelayMilliseconds)
    {
        var retryQueueName = GetRetryQueueName(queueName, routingKey);

        var retryQueueArguments = new Dictionary<string, object>
        {
            ["x-message-ttl"] = retryDelayMilliseconds,
            ["x-dead-letter-exchange"] = mainExchangeName,
            ["x-dead-letter-routing-key"] = routingKey
        };

        channel.QueueDeclare(
            queue: retryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: retryQueueArguments);

        channel.QueueBind(
            queue: retryQueueName,
            exchange: retryExchangeName,
            routingKey: routingKey);
    }

    /// <summary>
    /// Khai báo dead-letter queue cho một routing key.
    /// </summary>
    /// <remarks>
    /// Dead-letter queue lưu message không thể xử lý tự động sau khi vượt quá retry limit
    /// hoặc message không hợp lệ về payload/contract.
    ///
    /// Message trong DLQ cần được monitor và có quy trình xử lý sau đó,
    /// ví dụ replay, quarantine, cancel business flow hoặc escalate cho operator.
    /// </remarks>
    private static void DeclareDeadLetterQueue(
        IModel channel,
        string deadLetterExchangeName,
        string queueName,
        string routingKey)
    {
        var deadLetterQueueName = GetDeadLetterQueueName(queueName);

        channel.QueueDeclare(
            queue: deadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        channel.QueueBind(
            queue: deadLetterQueueName,
            exchange: deadLetterExchangeName,
            routingKey: routingKey);
    }

    /// <summary>
    /// Tạo suffix an toàn từ routing key để dùng trong tên retry queue.
    /// </summary>
    /// <remarks>
    /// Routing key có thể chứa ký tự không phù hợp hoặc quá dài để đưa trực tiếp vào tên queue.
    /// Nếu routing key quá dài, method này dùng hash ngắn để giữ tên queue ổn định và tránh vượt giới hạn.
    /// </remarks>
    private static string CreateSafeRoutingKeySuffix(string routingKey)
    {
        var safeText = Regex.Replace(routingKey, "[^a-zA-Z0-9._-]", "-");

        if (safeText.Length <= 120)
        {
            return safeText;
        }

        var hash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(routingKey)));

        return hash[..16];
    }
}