using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Domain.Service.Catalog.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(ECommerceDbContext dbContext)
    : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(item => item.Id == request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Category was not found.");

        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
