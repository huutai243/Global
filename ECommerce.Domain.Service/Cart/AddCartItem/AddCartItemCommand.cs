using MediatR;

namespace ECommerce.Domain.Service.Cart.AddCartItem;

public sealed record AddCartItemCommand(Guid CustomerId, Guid ProductId, int Quantity) : IRequest<CartResponse>;
