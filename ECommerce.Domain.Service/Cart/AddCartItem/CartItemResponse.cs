namespace ECommerce.Domain.Service.Cart.AddCartItem;

public sealed record CartItemResponse(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);
