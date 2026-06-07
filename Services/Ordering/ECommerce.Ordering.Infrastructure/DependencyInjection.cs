
using ECommerce.Ordering.Domain.Contracts.Cart;
using ECommerce.Ordering.Infrastructure.Clients;
using ECommerce.Ordering.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            throw new InvalidOperationException("ECommerce database connection string is not configured.");
        }

        services.AddDbContext<OrderingDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });
        services.AddScoped<DbContext>(serviceProvider => serviceProvider.GetRequiredService<OrderingDbContext>());

        return services;
    }

    public static IServiceCollection AddCartCheckoutClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseAddress = configuration["CartApi:BaseAddress"];

        if (string.IsNullOrWhiteSpace(baseAddress))
        {
            throw new InvalidOperationException("Cart API base address is not configured.");
        }

        services.AddHttpClient<ICartCheckoutClient, HttpCartCheckoutClient>(client =>
        {
            client.BaseAddress = new Uri(baseAddress);
        });

        return services;
    }
}
