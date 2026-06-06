namespace ECommerce.Catalog.WebApi.Controllers.Request;
public sealed class CreateCategoryFormRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public IFormFile? Image { get; init; }
}