using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Domain.Core.Catalog.Responses;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Domain.Service.Catalog.GetCategoryById;

public sealed class GetCategoryByIdQueryHandler(ECommerceDbContext dbContext)
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
