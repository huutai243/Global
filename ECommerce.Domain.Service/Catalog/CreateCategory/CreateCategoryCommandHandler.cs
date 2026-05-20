using ECommerce.Domain.Core.Catalog.Models;
using ECommerce.Domain.Core.Catalog.Responses;
using ECommerce.Infrastructure.Persistence;
using MediatR;

namespace ECommerce.Domain.Service.Catalog.CreateCategory;

public sealed class CreateCategoryCommandHandler(ECommerceDbContext dbContext)
    : IRequestHandler<CreateCategoryCommand, CategoryResponse>
{
    public async Task<CategoryResponse> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description,
            IsActive = true,
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
            IsActive = category.IsActive
        };
    }
}
