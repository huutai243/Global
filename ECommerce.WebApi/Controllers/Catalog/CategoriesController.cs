using ECommerce.Domain.Service.Catalog.CreateCategory;
using ECommerce.Domain.Service.Catalog.DeleteCategory;
using ECommerce.Domain.Service.Catalog.GetCategories;
using ECommerce.Domain.Service.Catalog.GetCategoryById;
using ECommerce.Domain.Service.Catalog.UpdateCategory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.WebApi.Controllers.Catalog;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoriesAsync(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        return Ok(await sender.Send(new GetCategoriesQuery(includeInactive), cancellationToken));
    }

    [HttpGet("{categoryId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryByIdAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetCategoryByIdQuery(categoryId), cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategoryAsync(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetCategoryByIdAsync), new { categoryId = response.Id }, response);
    }

    [HttpPut("{categoryId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategoryAsync(
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new UpdateCategoryCommand(categoryId, request.Name, request.Description, request.IsActive),
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{categoryId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCategoryCommand(categoryId), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateCategoryRequest(string Name, string? Description, bool IsActive);
