using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Models;
using ECommerce.Ordering.Domain.Models;
using ECommerce.Payment.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.BackgroundJobs;

public sealed class OutboxRetryJob(OutboxDispatcher dispatcher, ILogger<OutboxRetryJob> logger) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retry job retried pending outbox messages");
        await dispatcher.ExecuteAsync(cancellationToken);
    }
}

public sealed class OutboxReconcileJob(ECommerceDbContext dbContext, ILogger<OutboxReconcileJob> logger) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-10);
        var stuckMessages = await dbContext.OutboxMessages
            .Where(message => message.Status == OutboxStatus.Processing && message.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var message in stuckMessages)
        {
            message.Status = OutboxStatus.Pending;
            message.NextRetryAt = DateTime.UtcNow;
            logger.LogWarning("Reconcile job found stuck outbox message {OutboxMessageId}", message.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class PaymentOrderReconcileJob(ECommerceDbContext dbContext, ILogger<PaymentOrderReconcileJob> logger) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var mismatches = await dbContext.Payments
            .Where(payment => payment.Status == PaymentStatus.Succeeded)
            .Join(dbContext.Orders, payment => payment.OrderId, order => order.Id, (payment, order) => new { payment, order })
            .Where(item => item.order.Status != OrderStatus.Paid)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var item in mismatches)
        {
            logger.LogWarning(
                "Reconcile job found payment/order mismatch for order {OrderId}. TODO Phase 3: rely on PaymentSucceededEvent consumer instead of shared reconciliation updates.",
                item.order.Id);
        }
    }
}

public sealed class PendingPaymentReminderJob(ECommerceDbContext dbContext, ILogger<PendingPaymentReminderJob> logger) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-30);
        var orders = await dbContext.Orders
            .Where(order => order.Status == OrderStatus.PendingPayment && order.CreatedAt < cutoff)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var order in orders)
        {
            logger.LogInformation("Pending payment reminder for order {OrderId}", order.Id);
        }
    }
}

public sealed class IdempotencyCleanupJob(
    ECommerceDbContext dbContext,
    IOptions<CleanupJobSettings> options,
    ILogger<IdempotencyCleanupJob> logger) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-options.Value.IdempotencyRecordRetentionDays);
        var records = await dbContext.IdempotencyRecords
            .Where(record => record.CompletedAt != null && record.CompletedAt < cutoff)
            .Take(100)
            .ToListAsync(cancellationToken);
        dbContext.IdempotencyRecords.RemoveRange(records);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Cleanup job removed {Count} expired idempotency records", records.Count);
    }
}

public sealed class OutboxCleanupJob(
    ECommerceDbContext dbContext,
    IOptions<CleanupJobSettings> options,
    ILogger<OutboxCleanupJob> logger) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-options.Value.ProcessedOutboxRetentionDays);
        var messages = await dbContext.OutboxMessages
            .Where(message => message.Status == OutboxStatus.Processed && message.ProcessedAt < cutoff)
            .Take(100)
            .ToListAsync(cancellationToken);
        dbContext.OutboxMessages.RemoveRange(messages);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Cleanup job removed {Count} processed outbox messages", messages.Count);
    }
}

public sealed class AbandonedCartCleanupJob(
    ECommerceDbContext dbContext,
    IOptions<CleanupJobSettings> options,
    ILogger<AbandonedCartCleanupJob> logger) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-options.Value.AbandonedCartRetentionDays);
        var carts = await dbContext.Carts
            .Include(cart => cart.Items)
            .Where(cart => cart.UpdatedAt != null && cart.UpdatedAt < cutoff && cart.Items.Count > 0)
            .Take(50)
            .ToListAsync(cancellationToken);
        foreach (var cart in carts)
        {
            dbContext.CartItems.RemoveRange(cart.Items);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Cleanup job cleared {Count} abandoned carts", carts.Count);
    }
}

public sealed class RecalculateOrderTotalsBatchJob(ECommerceDbContext dbContext, ILogger<RecalculateOrderTotalsBatchJob> logger) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var processedCount = 0;
        var orders = await dbContext.Orders.Include(order => order.Items).Take(100).ToListAsync(cancellationToken);
        foreach (var order in orders)
        {
            order.TotalAmount = order.Items.Sum(item => item.LineTotal);
            processedCount++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Batch job recalculated totals for {ProcessedCount} orders", processedCount);
    }
}

public sealed class PaymentFailureCompensationJob(ECommerceDbContext dbContext, ILogger<PaymentFailureCompensationJob> logger) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var failedPayments = await dbContext.Payments
            .Where(payment => payment.Status == PaymentStatus.Failed)
            .Join(dbContext.Orders, payment => payment.OrderId, order => order.Id, (payment, order) => new { payment, order })
            .Where(item => item.order.Status == OrderStatus.PendingPayment)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var item in failedPayments)
        {
            logger.LogWarning(
                "Compensation found failed payment for pending order {OrderId}. TODO Phase 3: rely on PaymentFailedEvent consumer instead of shared compensation updates.",
                item.order.Id);
        }
    }
}

public abstract class SkeletonJob(ILogger logger, string message) : IBackgroundJob
{
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("{Message}", message);
        return Task.CompletedTask;
    }
}

public sealed class OrderCreatedProcessingJob(ILogger<OrderCreatedProcessingJob> logger) : SkeletonJob(logger, "Processing job skeleton for OrderCreatedEvent consumer.");

public sealed class PaymentSucceededProcessingJob(ILogger<PaymentSucceededProcessingJob> logger) : SkeletonJob(logger, "Processing job skeleton for PaymentSucceededEvent consumer.");

public sealed class PaymentFailedProcessingJob(ILogger<PaymentFailedProcessingJob> logger) : SkeletonJob(logger, "Processing job skeleton for PaymentFailedEvent consumer.");

public sealed class ProductCatalogSyncJob(ILogger<ProductCatalogSyncJob> logger) : SkeletonJob(logger, "Sync job skeleton for external product catalog.");

public sealed class InventorySyncJob(ILogger<InventorySyncJob> logger) : SkeletonJob(logger, "Sync job skeleton for external inventory system.");

public sealed class PaymentStatusPollingJob(ILogger<PaymentStatusPollingJob> logger) : SkeletonJob(logger, "Polling job skeleton for fake payment status.");

public sealed class DailyOrderExportJob(ILogger<DailyOrderExportJob> logger) : SkeletonJob(logger, "ETL/export job skeleton for daily order export.");

public sealed class SystemMonitoringJob(ILogger<SystemMonitoringJob> logger) : SkeletonJob(logger, "Monitoring job skeleton for outbox/order/infra warnings.");

public sealed class StockCompensationJob(ILogger<StockCompensationJob> logger) : SkeletonJob(logger, "Compensation job skeleton for future stock rollback.");

public sealed class BackfillOrderTotalJob(ILogger<BackfillOrderTotalJob> logger) : SkeletonJob(logger, "One-time backfill job skeleton for order totals.");

public sealed class BackfillCustomerProfileJob(ILogger<BackfillCustomerProfileJob> logger) : SkeletonJob(logger, "One-time backfill job skeleton for customer profiles.");
