using System.Text;
using System.Text.RegularExpressions;
using RabbitMQ.Client;

namespace ECommerce.Infrastructure.RabbitMq.Consumers;

public static class RabbitMqConsumerTopology
{
    private const string RetryExchangeSuffix = ".retry";
    private const string DeadLetterExchangeSuffix = ".dead";

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

    public static string GetRetryExchangeName(string exchangeName)
    {
        return $"{exchangeName}{RetryExchangeSuffix}";
    }

    public static string GetDeadLetterExchangeName(string exchangeName)
    {
        return $"{exchangeName}{DeadLetterExchangeSuffix}";
    }

    public static string GetRetryQueueName(string queueName, string routingKey)
    {
        return $"{queueName}.retry.{CreateSafeRoutingKeySuffix(routingKey)}";
    }

    public static string GetDeadLetterQueueName(string queueName)
    {
        return $"{queueName}.dead";
    }

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