using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Core.SharedLibs.Interfaces;
using ECommerce.Domain.Core.Catalog.Responses;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Domain.Service.Catalog.UpdateProduct;

public sealed class UpdateProductCommandHandler(ECommerceDbContext dbContext, IProductCache productCache)
    : IRequestHandler<UpdateProductCommand, ProductResponse>
{
    public async Task<ProductResponse> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(item => item.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");

        var category = await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.CategoryId && item.IsActive, cancellationToken);
        if (category is null)
        {
            throw new BusinessRuleException("Product must belong to an active category.");
        }

        product.CategoryId = request.CategoryId;
        product.Name = request.Name.Trim();
        product.Description = request.Description;
        product.Price = request.Price;
        product.Status = request.Status;
        product.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await productCache.RemoveAsync($"product:{product.Id}", cancellationToken);

        return new ProductResponse
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            CategoryName = category.Name,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Status = product.Status.ToString()
        };
    }
}
