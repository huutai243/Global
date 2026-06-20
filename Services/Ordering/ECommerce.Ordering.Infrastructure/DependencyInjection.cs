using ECommerce.Ordering.Domain.Contracts.Cart;
using ECommerce.Ordering.Infrastructure.Clients;
using ECommerce.Ordering.Infrastructure.Clients.Options;
using ECommerce.Ordering.Infrastructure.Persistence;
using ECommerce.Shared.Core.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ECommerce.Ordering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrderingConnection")
            ?? configuration.GetConnectionString("ECommerceConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Ordering database connection string is not configured.");
        }

        services.AddDbContext<OrderingDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<DbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<OrderingDbContext>());

        return services;
    }

    public static IServiceCollection AddCartCheckoutClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CartClientOptions>(
            configuration.GetSection(CartClientOptions.SectionName));

        services.AddHttpContextAccessor();
        services.AddTransient<ForwardAuthorizationHeaderHandler>();

        services
            .AddHttpClient<ICartCheckoutClient, HttpCartCheckoutClient>(
                (serviceProvider, client) =>
                {
                    var options = serviceProvider
                        .GetRequiredService<IOptions<CartClientOptions>>()
                        .Value;

                    if (string.IsNullOrWhiteSpace(options.BaseAddress))
                    {
                        throw new InvalidOperationException("CartClient:BaseAddress is not configured.");
                    }

                    client.BaseAddress = new Uri(options.BaseAddress);
                })
            .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>();

        return services;
    }
}