using ECommerce.Infrastructure.Kafka.Configuration;
using ECommerce.Infrastructure.Payment;
using ECommerce.Infrastructure.RabbitMq.Configuration;
using ECommerce.Payment.Application.PayOrder;
using ECommerce.Payment.Infrastructure;
using ECommerce.Payment.Worker.Consumers.Kafka;
using ECommerce.Payment.Worker.Options;
using ECommerce.Shared.Core.Helpers;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Observability;
using FluentValidation;

namespace ECommerce.Payment.Worker.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddPaymentWorkerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddInfrastructure(configuration)
            .AddApplicationServices()
            .AddWorkerOptions(configuration)
            .AddBackgroundWorkers();

        return services;
    }

    private static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPaymentInfrastructure(configuration);
        services.AddRabbitMqMessaging(configuration);
        services.AddKafkaMessaging(configuration);
        services.AddObservability();
        services.AddStripePaymentGateway(configuration);

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<PayOrderCommandValidator>();
        services.AddSingleton<IJsonHelper, JsonHelper>();
        services.AddScoped<PayOrderCommandHandler>();

        return services;
    }

    private static IServiceCollection AddWorkerOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PayOrderKafkaConsumerOptions>(
            configuration.GetSection(PayOrderKafkaConsumerOptions.SectionName));

        return services;
    }

    private static IServiceCollection AddBackgroundWorkers(this IServiceCollection services)
    {
        services.AddHostedService<PayOrderKafkaConsumer>();

        return services;
    }
}