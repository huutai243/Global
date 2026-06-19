using ECommerce.Catalog.Infrastructure;
using ECommerce.Catalog.Worker.Options;
using ECommerce.Infrastructure.RabbitMq;
using ECommerce.Shared.Observability;
using ECommerce.Shared.Outbox;

namespace ECommerce.Catalog.Worker.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddCatalogWorkerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddInfrastructure(configuration)
            .AddCrossCuttingServices()
            .AddWorkerOptions(configuration)
            .AddBackgroundWorkers();

        return services;
    }

    private static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCatalogInfrastructure(configuration);
        services.AddRabbitMqMessaging(configuration);

        return services;
    }

    private static IServiceCollection AddCrossCuttingServices(this IServiceCollection services)
    {
        services.AddObservability();
        services.AddSingleton<OutboxMessageFactory>();

        return services;
    }

    private static IServiceCollection AddWorkerOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OutboxOptions>(
            configuration.GetSection(OutboxOptions.SectionName));

        return services;
    }

    private static IServiceCollection AddBackgroundWorkers(this IServiceCollection services)
    {
        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}