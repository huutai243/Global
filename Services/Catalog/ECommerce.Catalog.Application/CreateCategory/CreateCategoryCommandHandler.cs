using ECommerce.Catalog.Domain.Models;
using ECommerce.Catalog.Domain.Responses;
using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Infrastructure.Storage;
using MediatR;

namespace ECommerce.Catalog.Application.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    CatalogDbContext dbContext,
    IBlobStorageService blobStorageService)
    : IRequestHandler<CreateCategoryCommand, CategoryResponse>
{
    public async Task<CategoryResponse> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        string? imageUrl = null;

        if (request.Image is not null)
        {
            var uploadRequest = new FileUploadRequest
            {
                Content = request.Image.Content,
                FileName = request.Image.FileName,
                ContentType = request.Image.ContentType,
                FolderPath = "categories"
            };

            var uploadResult = await blobStorageService.UploadAsync(
                uploadRequest,
                cancellationToken);

            imageUrl = uploadResult.Url;
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description,
            IsActive = true,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(category);
    }

    private static CategoryResponse MapToResponse(Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            ImageUrl = category.ImageUrl
        };
    }
}
