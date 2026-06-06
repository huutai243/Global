using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Models;
using ECommerce.Shared.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.BackgroundJobs;

public sealed class OutboxDispatcher(
    ECommerceDbContext dbContext,
    IRabbitMqPublisher rabbitMqPublisher,
    IOptions<OutboxSettings> options,
    ILogger<OutboxDispatcher> logger) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var now = DateTime.UtcNow;
        var messages = await dbContext.OutboxMessages
            .Where(message => message.Status == OutboxStatus.Pending && (message.NextRetryAt == null || message.NextRetryAt <= now))
            .OrderBy(message => message.CreatedAt)
            .Take(settings.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.Status = OutboxStatus.Processing;
            await dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                await rabbitMqPublisher.PublishAsync(message.EventType, message.Payload, cancellationToken);
                message.Status = OutboxStatus.Processed;
                message.ProcessedAt = DateTime.UtcNow;
                message.ErrorMessage = null;
                logger.LogInformation("Outbox publish succeeded for message {OutboxMessageId}", message.Id);
            }
            catch (Exception exception)
            {
                message.RetryCount += 1;
                message.ErrorMessage = exception.Message;
                message.Status = message.RetryCount >= settings.MaxRetryCount ? OutboxStatus.Failed : OutboxStatus.Pending;
                message.NextRetryAt = DateTime.UtcNow.AddSeconds(settings.RetryDelaySeconds * Math.Max(1, message.RetryCount));
                logger.LogWarning(exception, "Outbox publish failed for message {OutboxMessageId}", message.Id);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
