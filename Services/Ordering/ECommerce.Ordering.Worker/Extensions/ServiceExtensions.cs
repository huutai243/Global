using ECommerce.Infrastructure.AzureServiceBus;
using ECommerce.Shared.Observability;
using ECommerce.Ordering.Infrastructure;
using ECommerce.Ordering.Worker.Options;

namespace ECommerce.Ordering.Worker.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddOrderingWorkerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOrderingInfrastructure(configuration);
        services.AddAzureServiceBusMessaging(configuration);
        services.AddObservability();

        services.Configure<OutboxOptions>(
            configuration.GetSection(OutboxOptions.SectionName));

        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}
