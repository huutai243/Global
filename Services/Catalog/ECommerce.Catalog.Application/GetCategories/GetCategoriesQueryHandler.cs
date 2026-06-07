using ECommerce.Catalog.Domain.Responses;
using ECommerce.Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Application.GetCategories;

public sealed class GetCategoriesQueryHandler(CatalogDbContext dbContext)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyCollection<CategoryResponse>>
{
    public async Task<IReadOnlyCollection<CategoryResponse>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Categories.AsNoTracking();

        if (!request.IncludeInactive)
        {
            query = query.Where(category => category.IsActive);
        }

        return await query
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive
            })
            .ToArrayAsync(cancellationToken);
    }
}
