using ECommerce.Domain.Core.Catalog.Requests;
using ECommerce.Domain.Core.Catalog.Responses;

namespace ECommerce.Domain.Core.Catalog.Interfaces.Services;

public interface IProductService
{
    Task<ProductResponse> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductResponse>> GetProductsAsync(
        CancellationToken cancellationToken = default);
}
