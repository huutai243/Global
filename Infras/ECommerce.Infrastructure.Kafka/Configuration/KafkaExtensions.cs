using ECommerce.Infrastructure.Kafka.Publishing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.Kafka.Configuration;

public static class KafkaExtensions
{
    public static IServiceCollection AddKafkaMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<KafkaSettings>(configuration.GetSection(KafkaSettings.SectionName));
        services.AddSingleton<IKafkaPublisher, KafkaPublisher>();

        return services;
    }
}