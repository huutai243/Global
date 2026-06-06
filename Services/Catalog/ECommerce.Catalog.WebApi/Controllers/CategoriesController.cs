using ECommerce.Catalog.Domain.Responses;
using ECommerce.Identity.Domain.Models;
using ECommerce.Catalog.Application.CreateCategory;
using ECommerce.Catalog.Application.DeleteCategory;
using ECommerce.Catalog.Application.GetCategories;
using ECommerce.Catalog.Application.GetCategoryById;
using ECommerce.Catalog.Application.UpdateCategory;
using ECommerce.Catalog.WebApi.Controllers.Factories;
using ECommerce.Catalog.WebApi.Controllers.Request;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Catalog.WebApi.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyCollection<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CategoryResponse>>> GetCategoriesAsync([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var response = await sender.Send(new GetCategoriesQuery(includeInactive), cancellationToken);

        return Ok(response);
    }

    [HttpGet("{categoryId:guid}", Name = nameof(GetCategoryByIdAsync))]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> GetCategoryByIdAsync([FromRoute] Guid categoryId, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetCategoryByIdQuery(categoryId), cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    //[Authorize(Roles = UserRoles.Admin)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CategoryResponse>> CreateCategoryAsync([FromForm] CreateCategoryFormRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateCategoryCommand(
            request.Name,
            request.Description,
            FormFileUploadRequestFactory.Create(request.Image, "categories"));

        var response = await sender.Send(command, cancellationToken);

        return CreatedAtRoute(nameof(GetCategoryByIdAsync), new { categoryId = response.Id }, response);
    }

    [HttpPut("{categoryId:guid}")]
    [Authorize(Roles = UserRoles.Admin)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> UpdateCategoryAsync(
        [FromRoute] Guid categoryId,
        [FromForm] UpdateCategoryFormRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryCommand(
            categoryId,
            request.Name,
            request.Description,
            request.IsActive,
            FormFileUploadRequestFactory.Create(request.Image, "categories"));

        var response = await sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{categoryId:guid}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategoryAsync([FromRoute] Guid categoryId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCategoryCommand(categoryId), cancellationToken);

        return NoContent();
    }
}