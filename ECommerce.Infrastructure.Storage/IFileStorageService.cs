namespace ECommerce.Infrastructure.Storage;

public interface IFileStorageService
{
    Task<FileUploadResult> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(string blobName, CancellationToken cancellationToken);
}