using ECommerce.Infrastructure.Storage;

namespace ECommerce.WebAPI.Controllers.Catalog;

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