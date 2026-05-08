using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.Persistence.Extensions;

public static class PersistenceExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ECommerceDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ECommerceDatabase")));

        return services;
    }
}
