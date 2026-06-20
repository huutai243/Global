using ECommerce.Infrastructure.RabbitMq;
using ECommerce.Ordering.Application.InventoryReservation;
using ECommerce.Ordering.Infrastructure;
using ECommerce.Ordering.Worker.Consumers;
using ECommerce.Ordering.Worker.Options;
using ECommerce.Shared.Observability;

namespace ECommerce.Ordering.Worker.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddOrderingWorkerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddInfrastructure(configuration)
            .AddApplicationServices()
            .AddCrossCuttingServices()
            .AddWorkerOptions(configuration)
            .AddBackgroundWorkers();

        return services;
    }

    private static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOrderingInfrastructure(configuration);
        services.AddRabbitMqMessaging(configuration);

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<InventoryReservationResultHandler>();

        return services;
    }

    private static IServiceCollection AddCrossCuttingServices(this IServiceCollection services)
    {
        services.AddObservability();

        return services;
    }

    private static IServiceCollection AddWorkerOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OutboxOptions>(
            configuration.GetSection(OutboxOptions.SectionName));

        services.Configure<InventoryReservationResultConsumerOptions>(
            configuration.GetSection(InventoryReservationResultConsumerOptions.SectionName));

        return services;
    }

    private static IServiceCollection AddBackgroundWorkers(this IServiceCollection services)
    {
        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<InventoryReservationResultConsumer>();

        return services;
    }
}