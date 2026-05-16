using ECommerce.Domain.Core.Catalog.Models;
using ECommerce.Catalog.Service.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.WebApi.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicProductsAsync(
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
            new GetPublicProductsQuery(searchTerm, categoryId, minPrice, maxPrice, sortBy, descending, pageNumber, pageSize),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{productId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetProductByIdQuery(productId), cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProductAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetProductByIdAsync), new { productId = response.Id }, response);
    }

    [HttpPut("{productId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProductAsync(
        Guid productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new UpdateProductCommand(productId, request.CategoryId, request.Name, request.Description, request.Price, request.Status),
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{productId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteProductCommand(productId), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateProductRequest(Guid CategoryId, string Name, string? Description, decimal Price, ProductStatus Status);
