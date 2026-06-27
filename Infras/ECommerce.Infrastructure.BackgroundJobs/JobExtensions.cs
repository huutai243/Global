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
        return services;
    }
}
