using System.Text;
using System.Text.Json;
using ECommerce.Infrastructure.RabbitMq.Configuration;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ECommerce.Infrastructure.RabbitMq.Publishing;

/// <summary>
/// Publisher dùng để publish message sang RabbitMQ exchange.
/// </summary>
/// <remarks>
/// Publisher này dùng durable direct exchange, persistent message, mandatory publish và publisher confirm
/// để giảm rủi ro mất message ở tầng broker.
///
/// Lưu ý enterprise:
/// Publisher confirm chỉ xác nhận RabbitMQ broker đã nhận message, không có nghĩa downstream consumer đã xử lý thành công.
/// Vì vậy consumer vẫn phải idempotent và hệ thống vẫn cần retry / DLQ / reconciliation ở các flow quan trọng.
/// </remarks>
public sealed class RabbitMqPublisher(
    IOptions<RabbitMqSettings> options,
    IMessageNameResolver messageNameResolver,
    ILogger<RabbitMqPublisher> logger)
   : IRabbitMqPublisher, IMessagePublisher, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan PublisherConfirmTimeout = TimeSpan.FromSeconds(5);
    private readonly object _connectionLock = new();
    private IConnection? _connection;
    private bool _disposed;

    /// <summary>
    /// Publish raw payload sang RabbitMQ bằng routing key đã được truyền vào.
    /// </summary>
    /// <remarks>
    /// Overload này thường dùng cho polling OutboxProcessor hoặc legacy flow,
    /// khi payload đã được serialize sẵn trong OutboxMessage.
    ///
    /// Nếu không truyền MessageId / CorrelationId / CausationId,
    /// consumer sẽ thiếu metadata để trace và deduplicate chính xác.
    /// </remarks>
    public Task PublishAsync(
        string eventType,
        string payload,
        CancellationToken cancellationToken = default)
    {
        PublishRaw(
            eventType,
            payload,
            messageId: null,
            correlationId: null,
            causationId: null,
            cancellationToken);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Serialize message và publish sang RabbitMQ với metadata đầy đủ.
    /// </summary>
    /// <remarks>
    /// Routing key được resolve từ message type để consumer bind đúng event/command cần xử lý.
    ///
    /// MessageMetadata chứa MessageId, CorrelationId và CausationId.
    /// Đây là các metadata quan trọng cho tracing, idempotency và exactly-once business effect.
    /// </remarks>
    public Task PublishAsync<TMessage>(
        TMessage message,
        MessageMetadata metadata,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(metadata);

        var routingKey = messageNameResolver.ResolveMessageName(typeof(TMessage));
        var payload = JsonSerializer.Serialize(message, SerializerOptions);

        PublishRaw(
            routingKey,
            payload,
            metadata.MessageId,
            metadata.CorrelationId,
            metadata.CausationId,
            cancellationToken);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Publish message sang RabbitMQ và chờ publisher confirm.
    /// </summary>
    /// <remarks>
    /// Method này dùng mandatory publish để phát hiện message không route được tới queue nào.
    /// Sau khi publish, publisher chờ broker confirm trong một khoảng timeout.
    ///
    /// Nếu publish không được confirm hoặc message bị return vì unroutable,
    /// exception sẽ được throw để caller có thể retry hoặc đánh dấu OutboxMessage là Failed.
    ///
    /// Lưu ý:
    /// Publish thành công không đồng nghĩa với business flow đã hoàn tất.
    /// Nó chỉ xác nhận message đã được RabbitMQ nhận và route hợp lệ.
    /// </remarks>
    private void PublishRaw(
        string routingKey,
        string payload,
        string? messageId,
        string? correlationId,
        string? causationId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var settings = options.Value;
        var connection = GetOrCreateConnection(settings);

        using var channel = connection.CreateModel();

        DeclareExchange(channel, settings.ExchangeName);
        channel.ConfirmSelect();

        var returned = false;
        string? returnReason = null;

        void OnBasicReturn(object? sender, BasicReturnEventArgs args)
        {
            returned = true;
            returnReason = $"ReplyCode: {args.ReplyCode}, ReplyText: {args.ReplyText}";
        }

        channel.BasicReturn += OnBasicReturn;

        try
        {
            var properties = CreateProperties(
                channel,
                messageId,
                correlationId,
                causationId);

            var body = Encoding.UTF8.GetBytes(payload);

            channel.BasicPublish(
                exchange: settings.ExchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body);

            var confirmed = channel.WaitForConfirms(PublisherConfirmTimeout);

            if (!confirmed)
            {
                throw new InvalidOperationException(
                    $"RabbitMQ publish was not confirmed within {PublisherConfirmTimeout.TotalSeconds} seconds. RoutingKey: {routingKey}, MessageId: {messageId}");
            }

            if (returned)
            {
                throw new InvalidOperationException(
                    $"RabbitMQ message was returned as unroutable. RoutingKey: {routingKey}, MessageId: {messageId}, Reason: {returnReason}");
            }

            logger.LogDebug(
                "RabbitMQ publish confirmed. RoutingKey: {RoutingKey}, MessageId: {MessageId}, CorrelationId: {CorrelationId}",
                routingKey,
                messageId,
                correlationId);
        }
        finally
        {
            channel.BasicReturn -= OnBasicReturn;
        }
    }

    /// <summary>
    /// Lấy connection hiện tại hoặc tạo connection mới nếu chưa có / đã đóng.
    /// </summary>
    /// <remarks>
    /// Publisher giữ lại một connection dùng chung để tránh tạo TCP connection mới cho mỗi lần publish.
    /// Mỗi lần publish vẫn tạo channel riêng vì channel là đơn vị làm việc nhẹ hơn connection.
    ///
    /// Lock được dùng để tránh nhiều thread cùng lúc tạo nhiều connection mới.
    /// </remarks>
    private IConnection GetOrCreateConnection(RabbitMqSettings settings)
    {
        if (_connection?.IsOpen == true)
        {
            return _connection;
        }

        lock (_connectionLock)
        {
            if (_connection?.IsOpen == true)
            {
                return _connection;
            }

            _connection?.Dispose();

            var factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true
            };

            _connection = factory.CreateConnection();

            logger.LogInformation(
                "RabbitMQ publisher connection created. HostName: {HostName}, Port: {Port}, ExchangeName: {ExchangeName}",
                settings.HostName,
                settings.Port,
                settings.ExchangeName);

            return _connection;
        }
    }

    /// <summary>
    /// Khai báo RabbitMQ exchange dùng để publish message.
    /// </summary>
    /// <remarks>
    /// Exchange được khai báo durable để tồn tại sau khi RabbitMQ restart.
    /// Direct exchange route message dựa trên routing key.
    /// </remarks>
    private static void DeclareExchange(IModel channel, string exchangeName)
    {
        channel.ExchangeDeclare(
            exchange: exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);
    }

    /// <summary>
    /// Tạo RabbitMQ message properties trước khi publish.
    /// </summary>
    /// <remarks>
    /// Persistent message giúp RabbitMQ lưu message bền hơn khi queue/exchange durable.
    ///
    /// MessageId dùng cho idempotency / InboxMessage.
    /// CorrelationId dùng để trace toàn bộ business flow.
    /// CausationId dùng để biết message hiện tại được sinh ra từ message hoặc command nào trước đó.
    /// </remarks>
    private static IBasicProperties CreateProperties(
        IModel channel,
        string? messageId,
        string? correlationId,
        string? causationId)
    {
        var properties = channel.CreateBasicProperties();

        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = messageId;
        properties.CorrelationId = correlationId;
        properties.Headers = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(causationId))
        {
            properties.Headers["CausationId"] = causationId;
        }

        return properties;
    }

    /// <summary>
    /// Chặn publish sau khi publisher đã bị dispose.
    /// </summary>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Đóng RabbitMQ connection khi publisher bị dispose.
    /// </summary>
    /// <remarks>
    /// Lỗi shutdown trong Dispose được bỏ qua vì đây là cleanup path.
    /// Không nên làm fail application shutdown chỉ vì connection đã đóng hoặc broker không còn reachable.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _connection?.Close();
        }
        catch
        {
            // Bỏ qua lỗi shutdown trong quá trình dispose.
        }

        _connection?.Dispose();
    }
}