using ECommerce.Catalog.Infrastructure;
using ECommerce.Catalog.Worker.Options;
using ECommerce.Infrastructure.RabbitMq.Configuration;
using ECommerce.Shared.Core.Helpers;
using ECommerce.Shared.Core.Interfaces;
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
        services.AddSingleton<IJsonHelper, JsonHelper>();

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