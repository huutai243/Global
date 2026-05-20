using ECommerce.Domain.Core.Catalog.Responses;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Domain.Service.Catalog.GetCategories;

public sealed class GetCategoriesQueryHandler(ECommerceDbContext dbContext)
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
