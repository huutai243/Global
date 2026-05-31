using ECommerce.Domain.Core.Cart.Responses;
using MediatR;

namespace ECommerce.Domain.Service.Cart.GetCart;

public sealed record GetCartQuery : IRequest<CartResponse>;