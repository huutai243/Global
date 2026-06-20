using ECommerce.Inventory.Infrastructure.Persistence;
using ECommerce.Inventory.Worker.Options;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Core.Retry;
using ECommerce.Shared.Messaging;
using ECommerce.Shared.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ECommerce.Inventory.Worker;

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
        logger.LogInformation("Inventory outbox processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
                await DelayNextPollAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Inventory outbox processor failed while polling.");
                await DelayNextPollAsync(stoppingToken);
            }
        }

        logger.LogInformation("Inventory outbox processor stopped.");
    }

    private async Task DelayNextPollAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(
            TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
            cancellationToken);
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var messageNameResolver = scope.ServiceProvider.GetRequiredService<IMessageNameResolver>();

        var messageIds = await GetProcessableMessageIdsAsync(dbContext, cancellationToken);

        foreach (var messageId in messageIds)
        {
            var message = await ClaimMessageAsync(dbContext, messageId, cancellationToken);

            if (message is null)
            {
                continue;
            }

            try
            {
                await ProcessMessageAsync(
                    dbContext,
                    publisher,
                    messageNameResolver,
                    message,
                    cancellationToken);
            }
            finally
            {
                dbContext.ChangeTracker.Clear();
            }
        }
    }

    private async Task<IReadOnlyCollection<Guid>> GetProcessableMessageIdsAsync(
        InventoryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        return await GetProcessableMessages(dbContext, utcNow)
            .AsNoTracking()
            .OrderBy(message => message.CreatedAtUtc)
            .Select(message => message.Id)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);
    }

    private async Task<OutboxMessage?> ClaimMessageAsync(
        InventoryDbContext dbContext,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var affectedRows = await GetProcessableMessages(dbContext, utcNow)
            .Where(message => message.Id == messageId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.Status, OutboxMessageStatus.Processing)
                .SetProperty(message => message.ProcessingStartedAtUtc, (DateTime?)utcNow)
                .SetProperty(message => message.ErrorMessage, (string?)null),
                cancellationToken);

        if (affectedRows == 0)
        {
            return null;
        }

        return await dbContext.OutboxMessages
            .FirstOrDefaultAsync(message => message.Id == messageId, cancellationToken);
    }

    private IQueryable<OutboxMessage> GetProcessableMessages(
        InventoryDbContext dbContext,
        DateTime utcNow)
    {
        var processingTimeoutUtc = utcNow.AddSeconds(-_options.ProcessingTimeoutSeconds);

        return dbContext.OutboxMessages.Where(message =>
            (
                (message.Status == OutboxMessageStatus.Pending || message.Status == OutboxMessageStatus.Failed)
                && (message.NextRetryAtUtc == null || message.NextRetryAtUtc <= utcNow)
            )
            || (
                message.Status == OutboxMessageStatus.Processing
                && message.ProcessingStartedAtUtc != null
                && message.ProcessingStartedAtUtc <= processingTimeoutUtc
            ));
    }

    private async Task ProcessMessageAsync(
        InventoryDbContext dbContext,
        IMessagePublisher publisher,
        IMessageNameResolver messageNameResolver,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await PublishAsync(publisher, messageNameResolver, message, cancellationToken);

            message.Status = OutboxMessageStatus.Processed;
            message.ProcessedAtUtc = DateTime.UtcNow;
            message.NextRetryAtUtc = null;
            message.ErrorMessage = null;

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Processed inventory outbox message. MessageId: {MessageId}, CorrelationId: {CorrelationId}, MessageType: {MessageType}",
                message.MessageId,
                message.CorrelationId,
                message.MessageType);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogInformation(
                exception,
                "Skipped inventory outbox message because it was updated concurrently. MessageId: {MessageId}, CorrelationId: {CorrelationId}, MessageType: {MessageType}",
                message.MessageId,
                message.CorrelationId,
                message.MessageType);
        }
        catch (Exception exception)
        {
            await MarkFailedAsync(dbContext, message, exception, cancellationToken);
        }
    }

    private static async Task PublishAsync(
        IMessagePublisher publisher,
        IMessageNameResolver messageNameResolver,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        var inventoryReservedEventType = messageNameResolver.ResolveMessageName(
            typeof(InventoryReservedEvent));

        var inventoryReservationFailedEventType = messageNameResolver.ResolveMessageName(
            typeof(InventoryReservationFailedEvent));

        if (string.Equals(message.MessageType, inventoryReservedEventType, StringComparison.Ordinal))
        {
            var @event = Deserialize<InventoryReservedEvent>(message);
            var metadata = CreateMetadata(message);

            await publisher.PublishAsync(@event, metadata, cancellationToken);
            return;
        }

        if (string.Equals(message.MessageType, inventoryReservationFailedEventType, StringComparison.Ordinal))
        {
            var @event = Deserialize<InventoryReservationFailedEvent>(message);
            var metadata = CreateMetadata(message);

            await publisher.PublishAsync(@event, metadata, cancellationToken);
            return;
        }

        throw new InvalidOperationException(
            $"Unsupported inventory outbox message type '{message.MessageType}'.");
    }

    private static TMessage Deserialize<TMessage>(OutboxMessage message)
        where TMessage : class
    {
        var deserializedMessage = JsonSerializer.Deserialize<TMessage>(
            message.Payload,
            SerializerOptions);

        if (deserializedMessage is null)
        {
            throw new InvalidOperationException(
                $"Inventory outbox payload for message '{message.MessageId}' could not be deserialized.");
        }

        return deserializedMessage;
    }

    private static MessageMetadata CreateMetadata(OutboxMessage message)
    {
        return new MessageMetadata(
            message.MessageId,
            message.CorrelationId,
            message.CausationId,
            message.OccurredAtUtc);
    }

    private async Task MarkFailedAsync(
        InventoryDbContext dbContext,
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
                "Skipped inventory outbox failure update because it was updated concurrently. MessageId: {MessageId}, CorrelationId: {CorrelationId}, MessageType: {MessageType}",
                message.MessageId,
                message.CorrelationId,
                message.MessageType);

            return;
        }

        logger.LogWarning(
            exception,
            "Inventory outbox message publish failed. MessageId: {MessageId}, CorrelationId: {CorrelationId}, MessageType: {MessageType}, RetryCount: {RetryCount}, Status: {Status}",
            message.MessageId,
            message.CorrelationId,
            message.MessageType,
            message.RetryCount,
            message.Status);
    }
}