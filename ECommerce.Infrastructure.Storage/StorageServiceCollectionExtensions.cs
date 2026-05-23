using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.Storage;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddBlobStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<BlobStorageSettings>()
            .Bind(configuration.GetSection(BlobStorageSettings.SectionName))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.ConnectionString),
                "Blob storage connection string is required.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.ContainerName),
                "Blob storage container name is required.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.PublicBaseUrl),
                "Blob storage public base URL is required.")
            .Validate(settings => settings.MaxFileSizeInBytes > 0,
                "Blob storage max file size must be greater than zero.")
            .ValidateOnStart();

        services.AddSingleton<IBlobStorageService, AzureBlobFileStorageService>();

        return services;
    }
}