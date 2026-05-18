using ECommerce.Domain.Service.Cart.AddCartItem;
using ECommerce.Domain.Service.Ordering.CheckoutCart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.WebApi.Controllers.Cart;

[ApiController]
[Route("api/customers/{customerId:guid}/cart")]
[Authorize(Roles = "Customer,Admin")]
public class CartController(ISender sender) : ControllerBase
{
    [HttpPost("items")]
    public async Task<IActionResult> AddItemAsync(Guid customerId, AddCartItemRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new AddCartItemCommand(customerId, request.ProductId, request.Quantity), cancellationToken));
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> CheckoutAsync(Guid customerId, CheckoutRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new CheckoutCartCommand(customerId, request.IdempotencyKey), cancellationToken));
    }
}

public sealed record AddCartItemRequest(Guid ProductId, int Quantity);

public sealed record CheckoutRequest(string IdempotencyKey);
