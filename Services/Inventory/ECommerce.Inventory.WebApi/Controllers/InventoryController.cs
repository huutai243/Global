using ECommerce.Inventory.Application.GetProductAvailability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Inventory.WebApi.Controllers;

[ApiController]
[Route("api/inventory")]
public sealed class InventoryController(ISender sender) : ControllerBase
{
    [HttpGet("products/{productId:guid}/availability")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductAvailabilityAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new GetProductAvailabilityQuery(productId),
            cancellationToken);

        return Ok(response);
    }
}