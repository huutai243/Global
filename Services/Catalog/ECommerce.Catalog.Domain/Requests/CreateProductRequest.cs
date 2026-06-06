namespace ECommerce.Catalog.Domain.Requests;

public class CreateProductRequest
{
    public Guid CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int InitialStock { get; set; }
}
