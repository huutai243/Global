namespace ECommerce.Catalog.Core.Responses;

public class ProductResponse
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string Status { get; set; } = string.Empty;
}
