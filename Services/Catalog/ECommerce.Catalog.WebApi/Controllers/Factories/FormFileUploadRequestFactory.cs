using ECommerce.Infrastructure.Storage;

namespace ECommerce.Catalog.WebApi.Controllers.Factories;

public static class FormFileUploadRequestFactory
{
    public static FileUploadRequest? Create(IFormFile? file, string folderPath)
    {
        if (file is null)
        {
            return null;
        }

        return new FileUploadRequest
        {
            Content = file.OpenReadStream(),
            FileName = file.FileName,
            ContentType = file.ContentType,
            FolderPath = folderPath
        };
    }
}