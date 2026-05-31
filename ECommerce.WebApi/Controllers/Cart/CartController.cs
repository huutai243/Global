using ECommerce.Domain.Core.Cart.Responses;
using ECommerce.Domain.Service.Cart.AddCartItem;
using ECommerce.Domain.Service.Cart.GetCart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.WebApi.Controllers.Cart;

[ApiController]
[Authorize]
[Route("api/cart")]
[Produces("application/json")]
public sealed class CartController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetCartQuery(), cancellationToken);

        return Ok(response);
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItemAsync(
        [FromBody] AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new AddCartItemCommand(
                request.ProductId,
                request.Quantity),
            cancellationToken);

        return Ok(response);
    }
}