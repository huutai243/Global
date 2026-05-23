namespace ECommerce.Infrastructure.Storage;

public class BlobStorageSettings
{
    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = string.Empty;

    public string PublicBaseUrl { get; set; } = string.Empty;
}