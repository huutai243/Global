namespace ECommerce.Infrastructure.Storage;

public sealed class BlobStorageSettings
{
    public const string SectionName = "BlobStorage";

    public string ConnectionString { get; init; } = string.Empty;

    public string ContainerName { get; init; } = string.Empty;

    public string PublicBaseUrl { get; init; } = string.Empty;

    public long MaxFileSizeInBytes { get; init; } = 5 * 1024 * 1024;

    public string[] AllowedContentTypes { get; init; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];
}