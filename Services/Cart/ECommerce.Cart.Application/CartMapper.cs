using ECommerce.Cart.Domain.Responses;

namespace ECommerce.Cart.Application;

internal static class CartMapper
{
    public static CartResponse MapToResponse(ECommerce.Cart.Domain.Models.Cart cart)
    {
        var items = cart.Items
            .Select(cartItem => new CartItemResponse(
                cartItem.Id,
                cartItem.ProductId,
                cartItem.ProductNameSnapshot,
                cartItem.ProductImageUrlSnapshot,
                cartItem.UnitPriceSnapshot,
                cartItem.Quantity,
                cartItem.LineTotal))
            .ToArray();

        return new CartResponse(
            cart.Id,
            cart.CustomerId,
            items.Sum(item => item.LineTotal),
            items);
    }
}