using ECommerce.Core.SharedLibs.Interfaces;
using MediatR;

namespace ECommerce.Domain.Service.Payment.PayOrder;

public sealed record PayOrderCommand(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string PaymentMethod,
    string IdempotencyKey) : IRequest<PayOrderResponse>, IIdempotentCommand;
