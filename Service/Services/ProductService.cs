using ECommerce.Catalog.Core.Interfaces.Repositories;
using ECommerce.Catalog.Core.Interfaces.Services;
using ECommerce.Catalog.Core.Models;
using ECommerce.Catalog.Core.Requests;
using ECommerce.Catalog.Core.Responses;

namespace ECommerce.Catalog.Service.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductResponse> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Product name must not be empty.", nameof(request));
        }

        if (request.Price <= 0)
        {
            throw new ArgumentException("Product price must be greater than 0.", nameof(request));
        }

        if (request.InitialStock < 0)
        {
            throw new ArgumentException("Product stock quantity must not be negative.", nameof(request));
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description,
            Price = request.Price,
            CategoryId = request.CategoryId,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var createdProduct = await _productRepository.CreateProductAsync(product, cancellationToken);

        return MapToProductResponse(createdProduct);
    }

    public async Task<IReadOnlyCollection<ProductResponse>> GetProductsAsync(
        CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetProductsAsync(cancellationToken);

        return products.Select(MapToProductResponse).ToArray();
    }

    private static ProductResponse MapToProductResponse(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CategoryId = product.CategoryId,
            Status = product.Status.ToString()
        };
    }
}
