using ECommerce.Ordering.Application.CheckoutCart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Ordering.WebApi.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Roles = "Customer,Admin")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    [HttpPost("checkout-cart")]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CheckoutCartAsync(
        [FromBody] CheckoutCartCommand command,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);

        return Ok(response);
    }
}
