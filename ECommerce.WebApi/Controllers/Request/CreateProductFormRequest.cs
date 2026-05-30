namespace ECommerce.WebAPI.Controllers.Request;
public sealed class CreateProductFormRequest
{
    public Guid CategoryId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public int InitialStock { get; init; }

    public IFormFile? Image { get; init; }
}