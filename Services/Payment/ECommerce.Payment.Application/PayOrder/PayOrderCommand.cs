using ECommerce.Shared.Core.Interfaces;
using MediatR;

namespace ECommerce.Payment.Application.PayOrder;

public sealed record PayOrderCommand(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string PaymentMethod,
    string IdempotencyKey) : IRequest<PayOrderResponse>, IIdempotentCommand;
