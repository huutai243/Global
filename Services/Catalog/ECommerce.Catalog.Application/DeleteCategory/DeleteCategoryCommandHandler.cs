using ECommerce.Shared.Core.Exceptions;
using ECommerce.Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Application.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(CatalogDbContext dbContext)
    : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(item => item.Id == request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Category was not found.");

        // AUDIT NOTE:
        // Category deactivation is an administrative business action, but this handler only persists current state.
        // A real audit trail should record actor, action, entity id, old value, new value, correlation id, and timestamp.
        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
