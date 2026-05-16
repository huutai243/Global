using ECommerce.Domain.Core.Catalog.Models;
using ECommerce.Domain.Core.Catalog.Responses;

namespace ECommerce.Domain.Service.Catalog.Mapping;

public static class CatalogMapping
{
    public static ProductResponse MapProduct(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Status = product.Status.ToString()
        };
    }
}
