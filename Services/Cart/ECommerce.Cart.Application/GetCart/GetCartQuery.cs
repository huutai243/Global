using ECommerce.Cart.Domain.Responses;
using MediatR;

namespace ECommerce.Cart.Application.GetCart;

public sealed record GetCartQuery : IRequest<CartResponse>;