using ECommerce.Domain.Service.Catalog.CreateCategory;
using ECommerce.Domain.Service.Catalog.DeleteCategory;
using ECommerce.Domain.Service.Catalog.GetCategories;
using ECommerce.Domain.Service.Catalog.GetCategoryById;
using ECommerce.Domain.Service.Catalog.UpdateCategory;
using ECommerce.Infrastructure.Storage;
using ECommerce.WebAPI.Controllers.Catalog;
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

    [HttpGet("{categoryId:guid}", Name = nameof(GetCategoryByIdAsync))]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryByIdAsync(
    [FromRoute] Guid categoryId,
    CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetCategoryByIdQuery(categoryId), cancellationToken));
    }

    [HttpPost]
    //[Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateCategoryAsync(
        [FromForm] CreateCategoryFormRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCategoryCommand(
            request.Name,
            request.Description,
            FormFileUploadRequestFactory.Create(request.Image, "categories"));

        var response = await sender.Send(command, cancellationToken);

        return CreatedAtRoute(nameof(GetCategoryByIdAsync), new { categoryId = response.Id }, response);
    }

    [HttpPut("{categoryId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategoryAsync(
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new UpdateCategoryCommand(categoryId, request.Name, request.Description, request.IsActive, request.Image),
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

public sealed record UpdateCategoryRequest(string Name, string? Description, bool IsActive, FileUploadRequest? Image);
