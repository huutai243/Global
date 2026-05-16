using ECommerce.Core.SharedLibs.Interfaces;
using MediatR;

namespace ECommerce.Domain.Service.Ordering.CheckoutCart;

public sealed record CheckoutCartCommand(Guid CustomerId, string IdempotencyKey) : IRequest<CheckoutResponse>, IIdempotentCommand;
