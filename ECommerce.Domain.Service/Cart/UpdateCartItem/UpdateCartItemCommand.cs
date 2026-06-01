using ECommerce.Domain.Core.Cart.Responses;
using MediatR;

namespace ECommerce.Domain.Service.Cart.UpdateCartItem;

public sealed record UpdateCartItemCommand(Guid CartItemId, int Quantity) : IRequest<CartResponse>;