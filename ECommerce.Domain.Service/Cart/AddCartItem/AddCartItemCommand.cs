using ECommerce.Domain.Core.Cart.Responses;
using MediatR;

namespace ECommerce.Domain.Service.Cart.AddCartItem;

public sealed record AddCartItemCommand(Guid ProductId, int Quantity) : IRequest<CartResponse>;