using ECommerce.Shared.Core.Exceptions;
using ECommerce.Catalog.Domain.Responses;
using ECommerce.Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Application.GetCategoryById;

public sealed class GetCategoryByIdQueryHandler(CatalogDbContext dbContext)
    : IRequestHandler<GetCategoryByIdQuery, CategoryResponse>
{
    public async Task<CategoryResponse> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .Where(item => item.Id == request.CategoryId)
            .Select(item => new CategoryResponse
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                IsActive = item.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        return category ?? throw new NotFoundException("Category was not found.");
    }
}
