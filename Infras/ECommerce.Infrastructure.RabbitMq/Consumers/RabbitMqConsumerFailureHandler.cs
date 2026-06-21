using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.RabbitMq.Consumers;

public static class RabbitMqConsumerFailureHandler
{
    private const string RetryCountHeader = "RetryCount";

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