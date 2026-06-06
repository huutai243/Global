namespace ECommerce.Infrastructure.Storage;

public sealed class FileUploadRequest
{
    public Stream Content { get; init; } = Stream.Null;

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public string FolderPath { get; init; } = string.Empty;
}