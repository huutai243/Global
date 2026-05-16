namespace ECommerce.Domain.Service.Cart.AddCartItem;

public sealed record CartResponse(Guid CartId, Guid CustomerId, decimal TotalAmount, IReadOnlyCollection<CartItemResponse> Items);
