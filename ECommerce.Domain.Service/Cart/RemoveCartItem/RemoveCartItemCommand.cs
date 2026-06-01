using MediatR;

namespace ECommerce.Domain.Service.Cart.RemoveCartItem;

public sealed record RemoveCartItemCommand(Guid CartItemId) : IRequest;