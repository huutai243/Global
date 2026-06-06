using MediatR;

namespace ECommerce.Cart.Application.RemoveCartItem;

public sealed record RemoveCartItemCommand(Guid CartItemId) : IRequest;