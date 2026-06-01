using ECommerce.Domain.Core.Cart.Responses;
using ECommerce.Domain.Service.Cart.AddCartItem;
using ECommerce.Domain.Service.Cart.ClearCart;
using ECommerce.Domain.Service.Cart.GetCart;
using ECommerce.Domain.Service.Cart.RemoveCartItem;
using ECommerce.Domain.Service.Cart.UpdateCartItem;
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

    [HttpPut("items/{cartItemId:guid}")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItemAsync(
        Guid cartItemId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new UpdateCartItemCommand(
                cartItemId,
                request.Quantity),
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("items/{cartItemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItemAsync(
        Guid cartItemId,
        CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveCartItemCommand(cartItemId), cancellationToken);

        return NoContent();
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ClearAsync(CancellationToken cancellationToken)
    {
        await sender.Send(new ClearCartCommand(), cancellationToken);

        return NoContent();
    }
}