using ECommerce.Shared.Core.Retry;
using ECommerce.Shared.Messaging;
using ECommerce.Shared.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.BackgroundJobs;

public abstract class OutboxProcessorBase<TProcessor, TDbContext, TOptions>(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<TOptions> options,
    ILogger<TProcessor> logger)
    : BackgroundService
    where TDbContext : DbContext
    where TOptions : class, IOutboxProcessorOptions
{
    private readonly TOptions _options = options.Value;

    protected abstract string ProcessorName { get; }

    protected abstract Task PublishAsync(
        IMessagePublisher publisher,
        IMessageNameResolver messageNameResolver,
        OutboxMessage message,
        CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{ProcessorName} outbox processor started.", ProcessorName);

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
                logger.LogError(
                    exception,
                    "{ProcessorName} outbox processor failed while polling.",
                    ProcessorName);

                await DelayNextPollAsync(stoppingToken);
            }
        }

        logger.LogInformation("{ProcessorName} outbox processor stopped.", ProcessorName);
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

        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var messageNameResolver = scope.ServiceProvider.GetRequiredService<IMessageNameResolver>();

        var messageIds = await GetProcessableMessageIdsAsync(
            dbContext,
            cancellationToken);

        foreach (var messageId in messageIds)
        {
            var message = await ClaimMessageAsync(
                dbContext,
                messageId,
                cancellationToken);

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
        TDbContext dbContext,
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
        TDbContext dbContext,
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

        return await dbContext.Set<OutboxMessage>()
            .FirstOrDefaultAsync(message => message.Id == messageId, cancellationToken);
    }

    private IQueryable<OutboxMessage> GetProcessableMessages(
        TDbContext dbContext,
        DateTime utcNow)
    {
        var processingTimeoutUtc = utcNow.AddSeconds(-_options.ProcessingTimeoutSeconds);

        return dbContext.Set<OutboxMessage>().Where(message =>
            (
                (message.Status == OutboxMessageStatus.Pending ||
                 message.Status == OutboxMessageStatus.Failed)
                && (message.NextRetryAtUtc == null || message.NextRetryAtUtc <= utcNow)
            )
            || (
                message.Status == OutboxMessageStatus.Processing
                && message.ProcessingStartedAtUtc != null
                && message.ProcessingStartedAtUtc <= processingTimeoutUtc
            ));
    }

    private async Task ProcessMessageAsync(
        TDbContext dbContext,
        IMessagePublisher publisher,
        IMessageNameResolver messageNameResolver,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
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
                "Processed {ProcessorName} outbox message. MessageId: {MessageId}, CorrelationId: {CorrelationId}, MessageType: {MessageType}",
                ProcessorName,
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
                "Skipped {ProcessorName} outbox message because it was updated concurrently. MessageId: {MessageId}, CorrelationId: {CorrelationId}, MessageType: {MessageType}",
                ProcessorName,
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

    private async Task MarkFailedAsync(
        TDbContext dbContext,
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
                "Skipped {ProcessorName} outbox failure update because it was updated concurrently. MessageId: {MessageId}, CorrelationId: {CorrelationId}, MessageType: {MessageType}",
                ProcessorName,
                message.MessageId,
                message.CorrelationId,
                message.MessageType);

            return;
        }

        logger.LogWarning(
            exception,
            "{ProcessorName} outbox message publish failed. MessageId: {MessageId}, CorrelationId: {CorrelationId}, MessageType: {MessageType}, RetryCount: {RetryCount}, Status: {Status}",
            ProcessorName,
            message.MessageId,
            message.CorrelationId,
            message.MessageType,
            message.RetryCount,
            message.Status);
    }
}