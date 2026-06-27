using ECommerce.Shared.Core.Constants;
using ECommerce.Shared.Core.Exceptions;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Catalog.Domain.Models;
using ECommerce.Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Application.DeleteProduct;

public sealed class DeleteProductCommandHandler(CatalogDbContext dbContext, IProductCache productCache)
    : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(item => item.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");

        // AUDIT NOTE:
        // Product deactivation is an administrative business action, but this handler only persists current state.
        // A real audit trail should record actor, action, entity id, old value, new value, correlation id, and timestamp.
        product.Status = ProductStatus.Inactive;
        product.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await productCache.RemoveAsync(CacheKeyConstants.ProductById(request.ProductId), cancellationToken);
    }
}
