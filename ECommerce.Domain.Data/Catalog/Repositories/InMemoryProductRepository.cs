using ECommerce.Domain.Core.Catalog.Interfaces.Repositories;
using ECommerce.Domain.Core.Catalog.Models;

namespace ECommerce.Domain.Data.Catalog.Repositories;

public class InMemoryProductRepository : IProductRepository
{
    private static readonly List<Product> Products = [];
    private static readonly object SyncLock = new();

    public Task<Product> CreateProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);
        cancellationToken.ThrowIfCancellationRequested();

        var storedProduct = CloneProduct(product);

        lock (SyncLock)
        {
            Products.Add(storedProduct);
        }

        return Task.FromResult(CloneProduct(storedProduct));
    }

    public Task<IReadOnlyCollection<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Product[] products;

        lock (SyncLock)
        {
            products = Products.Select(CloneProduct).ToArray();
        }

        return Task.FromResult<IReadOnlyCollection<Product>>(products);
    }

    private static Product CloneProduct(Product product)
    {
        return new Product
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CategoryId = product.CategoryId,
            Status = product.Status,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
