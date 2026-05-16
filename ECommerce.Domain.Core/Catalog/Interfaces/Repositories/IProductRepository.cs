using ECommerce.Domain.Core.Catalog.Models;

namespace ECommerce.Domain.Core.Catalog.Interfaces.Repositories;

public interface IProductRepository
{
    Task<Product> CreateProductAsync(
        Product product,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Product>> GetProductsAsync(
        CancellationToken cancellationToken = default);
}
