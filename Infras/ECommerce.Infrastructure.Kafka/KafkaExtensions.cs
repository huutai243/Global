using ECommerce.Shared.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.Kafka;

public static class KafkaExtensions
{
    public static IServiceCollection AddKafkaMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaSettings>(configuration.GetSection(nameof(KafkaSettings)));
        services.AddScoped<IKafkaProducer, KafkaProducer>();
        return services;
    }
}
