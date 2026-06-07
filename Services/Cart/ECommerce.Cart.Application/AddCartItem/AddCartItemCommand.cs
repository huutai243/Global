using ECommerce.Cart.Domain.Responses;
using MediatR;

namespace ECommerce.Cart.Application.AddCartItem;

public sealed record AddCartItemCommand(
    Guid ProductId,
    int Quantity,
    string? ProductNameSnapshot = null,
    string? ProductImageUrlSnapshot = null,
    decimal? UnitPriceSnapshot = null) : IRequest<CartResponse>;
