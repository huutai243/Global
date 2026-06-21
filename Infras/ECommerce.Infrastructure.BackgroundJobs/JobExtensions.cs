using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.BackgroundJobs;

public static class JobExtensions
{
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BackgroundJobSettings>(configuration.GetSection(nameof(BackgroundJobSettings)));
        services.Configure<OutboxSettings>(configuration.GetSection(nameof(OutboxSettings)));
        services.Configure<CleanupJobSettings>(configuration.GetSection(nameof(CleanupJobSettings)));
        services.Configure<MonitoringJobSettings>(configuration.GetSection(nameof(MonitoringJobSettings)));
        services.AddScoped<PaymentOrderReconcileJob>();
        services.AddScoped<PendingPaymentReminderJob>();
        services.AddScoped<IdempotencyCleanupJob>();
        services.AddScoped<OutboxCleanupJob>();
        services.AddScoped<AbandonedCartCleanupJob>();
        services.AddScoped<RecalculateOrderTotalsBatchJob>();
        services.AddScoped<OrderCreatedProcessingJob>();
        services.AddScoped<PaymentSucceededProcessingJob>();
        services.AddScoped<PaymentFailedProcessingJob>();
        services.AddScoped<ProductCatalogSyncJob>();
        services.AddScoped<InventorySyncJob>();
        services.AddScoped<PaymentStatusPollingJob>();
        services.AddScoped<DailyOrderExportJob>();
        services.AddScoped<SystemMonitoringJob>();
        services.AddScoped<PaymentFailureCompensationJob>();
        services.AddScoped<StockCompensationJob>();
        services.AddScoped<BackfillOrderTotalJob>();
        services.AddScoped<BackfillCustomerProfileJob>();
        return services;
    }
}
