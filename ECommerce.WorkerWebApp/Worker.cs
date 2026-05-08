using ECommerce.Infrastructure.BackgroundJobs;

namespace ECommerce.WorkerWebApp;

public class Worker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
            var retryJob = scope.ServiceProvider.GetRequiredService<OutboxRetryJob>();
            var reconcileJob = scope.ServiceProvider.GetRequiredService<OutboxReconcileJob>();

            try
            {
                await dispatcher.ExecuteAsync(stoppingToken);
                await retryJob.ExecuteAsync(stoppingToken);
                await reconcileJob.ExecuteAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Worker job cycle failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
