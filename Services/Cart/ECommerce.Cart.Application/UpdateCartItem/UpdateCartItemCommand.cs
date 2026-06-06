using ECommerce.Cart.Domain.Responses;
using MediatR;

namespace ECommerce.Cart.Application.UpdateCartItem;

public sealed record UpdateCartItemCommand(Guid CartItemId, int Quantity) : IRequest<CartResponse>;