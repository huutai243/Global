using ECommerce.Catalog.Domain.Models;

namespace ECommerce.Catalog.WebApi.Controllers.Request;

public sealed class UpdateProductFormRequest
{
    public Guid CategoryId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public ProductStatus Status { get; init; }

    public IFormFile? Image { get; init; }
}