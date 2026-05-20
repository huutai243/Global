namespace ECommerce.Domain.Core.Catalog.Responses;

public class CategoryResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}
