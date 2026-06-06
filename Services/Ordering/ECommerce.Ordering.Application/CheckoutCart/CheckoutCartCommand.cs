using ECommerce.Shared.Core.Interfaces;
using MediatR;

namespace ECommerce.Ordering.Application.CheckoutCart;

public sealed record CheckoutCartCommand(string IdempotencyKey) : IRequest<CheckoutResponse>, IIdempotentCommand;