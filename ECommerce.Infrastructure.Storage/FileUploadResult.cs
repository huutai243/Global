namespace ECommerce.Infrastructure.Storage;

public sealed class FileUploadResult
{
    public string BlobName { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public long SizeInBytes { get; init; }
}