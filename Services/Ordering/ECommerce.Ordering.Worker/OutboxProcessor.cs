using ECommerce.Ordering.Infrastructure.Persistence;
using ECommerce.Ordering.Worker.Options;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Core.Retry;
using ECommerce.Shared.Messaging;
using ECommerce.Shared.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ECommerce.Ordering.Worker;

public sealed class OutboxProcessor(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Ordering outbox processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Ordering outbox processor failed while polling.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var messageNameResolver = scope.ServiceProvider.GetRequiredService<IMessageNameResolver>();

        var utcNow = DateTime.UtcNow;
        var processingTimeoutUtc = utcNow.AddSeconds(-_options.ProcessingTimeoutSeconds);

        var messages = await dbContext.OutboxMessages
            .Where(message =>
                (
                    (message.Status == OutboxMessageStatus.Pending || message.Status == OutboxMessageStatus.Failed)
                    && (message.NextRetryAtUtc == null || message.NextRetryAtUtc <= utcNow)
                )
                || (
                    message.Status == OutboxMessageStatus.Processing
                    && message.ProcessingStartedAtUtc != null
                    && message.ProcessingStartedAtUtc <= processingTimeoutUtc
                ))
            .OrderBy(message => message.CreatedAtUtc)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(
                dbContext,
                publisher,
                messageNameResolver,
                message,
                cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(
        OrderingDbContext dbContext,
        IMessagePublisher publisher,
        IMessageNameResolver messageNameResolver,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            message.Status = OutboxMessageStatus.Processing;
            message.ProcessingStartedAtUtc = DateTime.UtcNow;
            message.ErrorMessage = null;

            await dbContext.SaveChangesAsync(cancellationToken);

            await PublishAsync(
                publisher,
                messageNameResolver,
                message,
                cancellationToken);

            message.Status = OutboxMessageStatus.Processed;
            message.ProcessedAtUtc = DateTime.UtcNow;
            message.NextRetryAtUtc = null;
            message.ErrorMessage = null;

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Processed outbox message. MessageId: {MessageId}, CorrelationId: {CorrelationId}, MessageType: {MessageType}",
                message.MessageId,
                message.CorrelationId,
                message.MessageType);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogInformation(
                exception,
                "Skipped outbox message because it was updated concurrently. MessageId: {MessageId}, CorrelationId: {CorrelationId}, MessageType: {MessageType}",
                message.MessageId,
                message.CorrelationId,
                message.MessageType);
        }
        catch (Exception exception)
        {
            await MarkFailedAsync(
                dbContext,
                message,
                exception,
                cancellationToken);
        }
    }

    private static async Task PublishAsync(
        IMessagePublisher publisher,
        IMessageNameResolver messageNameResolver,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        var expectedMessageType = messageNameResolver.ResolveMessageName(typeof(ReserveInventoryCommand));

        if (!string.Equals(message.MessageType, expectedMessageType, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported outbox message type '{message.MessageType}'.");
        }

        var command = JsonSerializer.Deserialize<ReserveInventoryCommand>(
            message.Payload,
            SerializerOptions);

        if (command is null)
        {
            throw new InvalidOperationException($"Outbox payload for message '{message.MessageId}' could not be deserialized.");
        }

        var metadata = new MessageMetadata(
            message.MessageId,
            message.CorrelationId,
            message.CausationId,
            message.OccurredAtUtc);

        await publisher.PublishAsync(
            command,
            metadata,
            cancellationToken);
    }

    private async Task MarkFailedAsync(
        OrderingDbContext dbContext,
        OutboxMessage message,
        Exception exception,
        CancellationToken cancellationToken)
    {
        message.RetryCount++;
        message.ErrorMessage = exception.Message;

        var maxRetryCount = message.MaxRetryCount > 0
            ? message.MaxRetryCount
            : _options.MaxRetryCount;

        if (message.RetryCount >= maxRetryCount)
        {
            message.Status = OutboxMessageStatus.DeadLettered;
            message.DeadLetteredAtUtc = DateTime.UtcNow;
            message.NextRetryAtUtc = null;
        }
        else
        {
            var delaySeconds = ExponentialBackoffCalculator.GetDelaySeconds(
                message.RetryCount,
                _options.InitialRetryDelaySeconds,
                _options.MaxRetryDelaySeconds);

            message.Status = OutboxMessageStatus.Failed;
            message.NextRetryAtUtc = DateTime.UtcNow.AddSeconds(delaySeconds);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException concurrencyException)
        {
            logger.LogInformation(
                concurrencyException,
                "Skipped outbox failure update because it was updated concurrently. MessageId: {MessageId}, CorrelationId: {CorrelationId}, MessageType: {MessageType}",
                message.MessageId,
                message.CorrelationId,
                message.MessageType);

            return;
        }

        logger.LogWarning(
            exception,
            "Outbox message publish failed. MessageId: {MessageId}, CorrelationId: {CorrelationId}, MessageType: {MessageType}, RetryCount: {RetryCount}, Status: {Status}",
            message.MessageId,
            message.CorrelationId,
            message.MessageType,
            message.RetryCount,
            message.Status);
    }
}