using ECommerce.Infrastructure.Kafka.Configuration;
using ECommerce.Infrastructure.RabbitMq.Configuration;
using ECommerce.Ordering.Application.InventoryReservation;
using ECommerce.Ordering.Infrastructure;
using ECommerce.Ordering.Worker.Consumers;
using ECommerce.Ordering.Worker.Consumers.Kafka;
using ECommerce.Ordering.Worker.Options;
using ECommerce.Shared.Core.Helpers;
using ECommerce.Shared.Core.Interfaces;
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

        // RabbitMQ legacy/polling flow.
        services.AddRabbitMqMessaging(configuration);

        // CDC/Kafka core flow.
        services.AddKafkaMessaging(configuration);

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<InventoryReservationResultHandler>();
        services.AddSingleton<IJsonHelper, JsonHelper>();

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
        services.AddRabbitMqWorkerOptions(configuration);
        services.AddKafkaWorkerOptions(configuration);

        return services;
    }

    private static IServiceCollection AddRabbitMqWorkerOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OutboxOptions>(
            configuration.GetSection(OutboxOptions.SectionName));

        services.Configure<InventoryReservationResultConsumerOptions>(
            configuration.GetSection(InventoryReservationResultConsumerOptions.SectionName));

        return services;
    }

    private static IServiceCollection AddKafkaWorkerOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<InventoryReservationResultKafkaConsumerOptions>(
            configuration.GetSection(InventoryReservationResultKafkaConsumerOptions.SectionName));

        return services;
    }

    private static IServiceCollection AddBackgroundWorkers(this IServiceCollection services)
    {
        // Core checkout CDC/Kafka flow.
        services.AddKafkaWorkers();

        // RabbitMQ legacy/polling flow.
        // Mở lại group này nếu muốn quay về RabbitMQ.
        // services.AddRabbitMqWorkers();

        return services;
    }

    private static IServiceCollection AddKafkaWorkers(this IServiceCollection services)
    {
        services.AddHostedService<InventoryReservationResultKafkaConsumer>();

        return services;
    }

    private static IServiceCollection AddRabbitMqWorkers(this IServiceCollection services)
    {
        // Polling outbox publisher: OrderingDb.OutboxMessages → RabbitMQ.
        services.AddHostedService<OutboxProcessor>();

        // RabbitMQ result consumer: Inventory → RabbitMQ → Ordering.
        services.AddHostedService<InventoryReservationResultConsumer>();

        return services;
    }
}