using ECommerce.Infrastructure.RabbitMq;
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
        services.AddRabbitMqMessaging(configuration);
        services.AddObservability();

        services.Configure<OutboxOptions>(
            configuration.GetSection(OutboxOptions.SectionName));

        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}