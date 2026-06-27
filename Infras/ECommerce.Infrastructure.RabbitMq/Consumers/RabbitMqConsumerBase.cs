using System.Text;
using ECommerce.Infrastructure.RabbitMq.Configuration;
using ECommerce.Shared.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ECommerce.Infrastructure.RabbitMq.Consumers;

/// <summary>
/// Base consumer dùng để consume message từ RabbitMQ queue theo cơ chế at-least-once delivery.
/// </summary>
/// <remarks>
/// RabbitMQ không đảm bảo exactly-once delivery ở tầng kỹ thuật.
/// Message có thể bị deliver lại nếu consumer xử lý xong business nhưng chưa kịp ack, hoặc connection bị lỗi.
/// Vì vậy các handler kế thừa class này phải xử lý idempotent bằng InboxMessage, unique constraint,
/// hoặc state transition có kiểm soát để đạt exactly-once business effect.
/// </remarks>
public abstract class RabbitMqConsumerBase<TConsumer>(
    RabbitMqSettings rabbitMqSettings,
    ILogger<TConsumer> logger)
    : BackgroundService
{
    private IConnection? _connection;
    private IModel? _channel;
    private CancellationToken _stoppingToken;

    protected abstract string QueueName { get; }

    protected abstract IReadOnlyCollection<string> RoutingKeys { get; }

    protected abstract ushort PrefetchCount { get; }

    protected abstract int MaxRetryCount { get; }

    protected abstract int RetryDelaySeconds { get; }

    /// <summary>
    /// Khởi tạo RabbitMQ connection, declare topology, cấu hình QoS và bắt đầu consume message.
    /// </summary>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        var factory = new ConnectionFactory
        {
            HostName = rabbitMqSettings.HostName,
            Port = rabbitMqSettings.Port,
            UserName = rabbitMqSettings.UserName,
            Password = rabbitMqSettings.Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };

        _connection = factory.CreateConnection();

        var channel = _connection.CreateModel();
        _channel = channel;

        RabbitMqConsumerTopology.DeclareConsumerTopology(
            channel,
            rabbitMqSettings.ExchangeName,
            QueueName,
            RoutingKeys,
            RetryDelaySeconds);

        ConfigureQos(channel);
        StartConsuming(channel);

        logger.LogInformation(
            "RabbitMQ consumer started. Consumer: {Consumer}, QueueName: {QueueName}, ExchangeName: {ExchangeName}, RoutingKeys: {RoutingKeys}, MaxRetryCount: {MaxRetryCount}, RetryDelaySeconds: {RetryDelaySeconds}",
            typeof(TConsumer).Name,
            QueueName,
            rabbitMqSettings.ExchangeName,
            string.Join(", ", RoutingKeys),
            MaxRetryCount,
            RetryDelaySeconds);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Cấu hình số lượng message tối đa mà consumer nhận trước khi ack.
    /// </summary>
    /// <remarks>
    /// PrefetchCount giúp tránh việc một consumer nhận quá nhiều message cùng lúc,
    /// đồng thời giới hạn mức độ song song và áp lực lên database / handler.
    /// </remarks>
    private void ConfigureQos(IModel channel)
    {
        channel.BasicQos(prefetchSize: 0, prefetchCount: PrefetchCount, global: false);
    }

    private void StartConsuming(IModel channel)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += ProcessMessageAsync;

        channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
    }

    /// <summary>
    /// Xử lý một RabbitMQ message, ack sau khi handler xử lý thành công,
    /// hoặc đẩy message sang retry / dead-letter flow khi xử lý lỗi.
    /// </summary>
    /// <remarks>
    /// Ack chỉ được gọi sau khi business handler hoàn tất.
    /// Nếu handler lỗi, message sẽ được retry hoặc đưa vào dead-letter queue tùy số lần retry.
    /// Invalid message được đưa thẳng vào dead-letter queue vì retry lại cũng không xử lý được.
    /// </remarks>
    private async Task ProcessMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        var channel = _channel;

        if (channel is null)
        {
            logger.LogError(
                "RabbitMQ channel is null. Consumer: {Consumer}, DeliveryTag: {DeliveryTag}",
                typeof(TConsumer).Name,
                args.DeliveryTag);

            return;
        }

        var payload = Encoding.UTF8.GetString(args.Body.ToArray());

        try
        {
            await HandleMessageAsync(args, payload, _stoppingToken);

            channel.BasicAck(deliveryTag: args.DeliveryTag, multiple: false);
        }
        catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
        {
            channel.BasicNack(deliveryTag: args.DeliveryTag, multiple: false, requeue: true);
        }
        catch (InvalidRabbitMqMessageException exception)
        {
            logger.LogWarning(
                exception,
                "RabbitMQ invalid message moved to DLQ. Consumer: {Consumer}, DeliveryTag: {DeliveryTag}, RoutingKey: {RoutingKey}, MessageId: {MessageId}, CorrelationId: {CorrelationId}",
                typeof(TConsumer).Name,
                args.DeliveryTag,
                args.RoutingKey,
                args.BasicProperties.MessageId,
                args.BasicProperties.CorrelationId);

            RabbitMqConsumerFailureHandler.DeadLetterInvalidMessage(
                channel,
                args,
                rabbitMqSettings.ExchangeName,
                logger,
                exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "RabbitMQ consumer failed. Consumer: {Consumer}, DeliveryTag: {DeliveryTag}, RoutingKey: {RoutingKey}, MessageId: {MessageId}, CorrelationId: {CorrelationId}",
                typeof(TConsumer).Name,
                args.DeliveryTag,
                args.RoutingKey,
                args.BasicProperties.MessageId,
                args.BasicProperties.CorrelationId);

            // TODO:
            // Theo dõi retry queue / dead-letter queue để phát hiện poison message.
            // Cần bổ sung reconciliation hoặc operator tooling để replay, quarantine hoặc xử lý thủ công.
            RabbitMqConsumerFailureHandler.RetryOrDeadLetter(
                channel,
                args,
                rabbitMqSettings.ExchangeName,
                MaxRetryCount,
                logger,
                exception);
        }
    }

    /// <summary>
    /// Xử lý business logic cho message đã consume từ RabbitMQ.
    /// </summary>
    /// <remarks>
    /// Method này phải được implement theo hướng idempotent.
    /// RabbitMQ có thể deliver cùng một message nhiều hơn một lần.
    /// Không được giả định rằng mỗi message chỉ được xử lý đúng một lần ở tầng kỹ thuật.
    /// </remarks>
    protected abstract Task HandleMessageAsync(
        BasicDeliverEventArgs args,
        string payload,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deserialize payload và chuyển lỗi deserialize thành invalid message exception.
    /// </summary>
    /// <remarks>
    /// Invalid payload thường là lỗi format hoặc contract mismatch.
    /// Những message này nên được đưa vào dead-letter queue thay vì retry vô hạn.
    /// </remarks>
    protected static TMessage DeserializePayloadRequired<TMessage>(
        IJsonHelper jsonHelper,
        string payload,
        string messageName)
        where TMessage : class
    {
        try
        {
            return jsonHelper.DeserializeRequired<TMessage>(
                payload,
                $"{messageName} payload could not be deserialized.");
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidRabbitMqMessageException(exception.Message);
        }
    }

    protected static InvalidRabbitMqMessageException CreateInvalidMessageException(string message)
    {
        return new InvalidRabbitMqMessageException(message);
    }

    /// <summary>
    /// Đóng RabbitMQ channel và connection khi worker dừng.
    /// </summary>
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();

        _channel?.Dispose();
        _connection?.Dispose();

        return base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Exception dùng cho message không hợp lệ về format, payload hoặc contract.
    /// </summary>
    /// <remarks>
    /// Invalid message không nên retry nhiều lần vì thường không thể tự hồi phục.
    /// Message sẽ được đưa vào dead-letter queue để kiểm tra sau.
    /// </remarks>
    public sealed class InvalidRabbitMqMessageException(string message) : Exception(message);
}