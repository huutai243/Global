namespace ECommerce.WebAPI.Controllers.Request;

public sealed class UpdateCategoryFormRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsActive { get; init; }

    public IFormFile? Image { get; init; }
}