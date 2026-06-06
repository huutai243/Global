using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Storage;

public sealed class AzureBlobFileStorageService : IBlobStorageService
{
    private readonly BlobStorageSettings _settings;
    private readonly BlobContainerClient _containerClient;

    public AzureBlobFileStorageService(IOptions<BlobStorageSettings> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _settings = options.Value;

        _containerClient = new BlobContainerClient(
            _settings.ConnectionString,
            _settings.ContainerName);
    }

    public async Task<FileUploadResult> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken)
    {
        ValidateUploadRequest(request);

        await _containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        string blobName = BuildBlobName(request.FolderPath, request.FileName);

        BlobClient blobClient = _containerClient.GetBlobClient(blobName);

        if (request.Content.CanSeek)
        {
            request.Content.Position = 0;
        }

        BlobUploadOptions uploadOptions = new()
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = request.ContentType
            },
            Metadata = new Dictionary<string, string>
            {
                ["originalFileName"] = request.FileName,
                ["uploadedAtUtc"] = DateTime.UtcNow.ToString("O")
            }
        };

        await blobClient.UploadAsync(request.Content, uploadOptions, cancellationToken);

        return new FileUploadResult
        {
            BlobName = blobName,
            Url = BuildPublicUrl(blobName),
            SizeInBytes = request.Content.CanSeek ? request.Content.Length : 0
        };
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return;
        }

        BlobClient blobClient = _containerClient.GetBlobClient(blobName);

        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private void ValidateUploadRequest(FileUploadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Content == Stream.Null)
        {
            throw new InvalidOperationException("Upload content is required.");
        }

        if (request.Content.CanSeek && request.Content.Length == 0)
        {
            throw new InvalidOperationException("Uploaded file is empty.");
        }

        if (request.Content.CanSeek && request.Content.Length > _settings.MaxFileSizeInBytes)
        {
            throw new InvalidOperationException("Uploaded file exceeds the allowed size.");
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new InvalidOperationException("File name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            throw new InvalidOperationException("Content type is required.");
        }

        bool isAllowedContentType = _settings.AllowedContentTypes
            .Any(contentType => string.Equals(
                contentType,
                request.ContentType,
                StringComparison.OrdinalIgnoreCase));

        if (!isAllowedContentType)
        {
            throw new InvalidOperationException("Unsupported image content type.");
        }

        string extension = Path.GetExtension(request.FileName);

        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new InvalidOperationException("File extension is required.");
        }
    }

    private static string BuildBlobName(string folderPath, string originalFileName)
    {
        string sanitizedFolderPath = NormalizeFolderPath(folderPath);
        string extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        string fileName = $"{Guid.NewGuid():N}{extension}";

        if (string.IsNullOrWhiteSpace(sanitizedFolderPath))
        {
            return fileName;
        }

        return $"{sanitizedFolderPath}/{fileName}";
    }

    private static string NormalizeFolderPath(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return string.Empty;
        }

        return folderPath
            .Replace("\\", "/", StringComparison.Ordinal)
            .Trim('/')
            .ToLowerInvariant();
    }

    private string BuildPublicUrl(string blobName)
    {
        return $"{_settings.PublicBaseUrl.TrimEnd('/')}/{blobName}";
    }
}