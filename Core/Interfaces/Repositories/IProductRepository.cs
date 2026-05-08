using ECommerce.Catalog.Core.Models;

namespace ECommerce.Catalog.Core.Interfaces.Repositories;

public interface IProductRepository
{
    Task<Product> CreateProductAsync(
        Product product,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Product>> GetProductsAsync(
        CancellationToken cancellationToken = default);
}
