using ECommerce.Catalog.Domain.Responses;
using ECommerce.Identity.Domain.Models;
using ECommerce.Catalog.Application.CreateProduct;
using ECommerce.Catalog.Application.DeleteProduct;
using ECommerce.Catalog.Application.GetProductById;
using ECommerce.Catalog.Application.GetPublicProducts;
using ECommerce.Catalog.Application.UpdateProduct;
using ECommerce.Catalog.WebApi.Controllers.Factories;
using ECommerce.Catalog.WebApi.Controllers.Request;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Catalog.WebApi.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> GetPublicProductsAsync(
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? categoryId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? sortBy,
        [FromQuery] bool descending,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await sender.Send(
            new GetPublicProductsQuery(
                searchTerm,
                categoryId,
                minPrice,
                maxPrice,
                sortBy,
                descending,
                pageNumber,
                pageSize),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{productId:guid}", Name = nameof(GetProductByIdAsync))]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetProductByIdAsync([FromRoute] Guid productId, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetProductByIdQuery(productId), cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = UserRoles.Admin)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> CreateProductAsync([FromForm] CreateProductFormRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.CategoryId,
            request.Name,
            request.Description,
            request.Price,
            request.InitialStock,
            FormFileUploadRequestFactory.Create(request.Image, "products"));

        var response = await sender.Send(command, cancellationToken);

        return CreatedAtRoute(nameof(GetProductByIdAsync), new { productId = response.Id }, response);
    }

    [HttpPut("{productId:guid}")]
    [Authorize(Roles = UserRoles.Admin)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> UpdateProductAsync([FromRoute] Guid productId, [FromForm] UpdateProductFormRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(
            productId,
            request.CategoryId,
            request.Name,
            request.Description,
            request.Price,
            request.Status,
            FormFileUploadRequestFactory.Create(request.Image, "products"));

        var response = await sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{productId:guid}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProductAsync([FromRoute] Guid productId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteProductCommand(productId), cancellationToken);

        return NoContent();
    }
}