using ECommerce.Shared.Core.Constants;
using ECommerce.Shared.Core.Exceptions;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Catalog.Domain.Models;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Application.DeleteProduct;

public sealed class DeleteProductCommandHandler(ECommerceDbContext dbContext, IProductCache productCache)
    : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(item => item.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");

        product.Status = ProductStatus.Inactive;
        product.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await productCache.RemoveAsync(CacheKeyConstants.ProductById(request.ProductId), cancellationToken);
    }
}
