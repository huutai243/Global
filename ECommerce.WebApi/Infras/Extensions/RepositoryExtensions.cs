using ECommerce.Core.SharedLibs.Helpers;
using ECommerce.Infrastructure.Storage;
using ECommerce.Core.SharedLibs.Interfaces;

namespace ECommerce.WebApi.Infras.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IJsonHelper, JsonHelper>();
        services.AddScoped<IBlobStorageService, AzureBlobFileStorageService>();

        return services;
    }
}
