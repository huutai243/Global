using ECommerce.Shared.Core.Exceptions;
using ECommerce.Catalog.Domain.Responses;
using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Infrastructure.Storage;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Application.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(
    CatalogDbContext dbContext,
    IBlobStorageService blobStorageService)
    : IRequestHandler<UpdateCategoryCommand, CategoryResponse>
{
    public async Task<CategoryResponse> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(
                item => item.Id == request.CategoryId,
                cancellationToken)
            ?? throw new NotFoundException("Category was not found.");

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

            category.ImageUrl = uploadResult.Url;
        }

        category.Name = request.Name.Trim();
        category.Description = request.Description;
        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

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
