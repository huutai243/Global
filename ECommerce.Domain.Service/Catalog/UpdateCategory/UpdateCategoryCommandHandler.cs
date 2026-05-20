using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Domain.Core.Catalog.Responses;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Domain.Service.Catalog.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(ECommerceDbContext dbContext)
    : IRequestHandler<UpdateCategoryCommand, CategoryResponse>
{
    public async Task<CategoryResponse> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(item => item.Id == request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Category was not found.");

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
            IsActive = category.IsActive
        };
    }
}
