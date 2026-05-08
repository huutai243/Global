using ECommerce.Catalog.Core.Requests;
using ECommerce.Catalog.Core.Responses;

namespace ECommerce.Catalog.Core.Interfaces.Services;

public interface IProductService
{
    Task<ProductResponse> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductResponse>> GetProductsAsync(
        CancellationToken cancellationToken = default);
}
