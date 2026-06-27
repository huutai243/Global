using ECommerce.Shared.Core.Interfaces;
using MediatR;

namespace ECommerce.Payment.Application.PayOrder;

// IDEMPOTENCY NOTE:
// This command carries an idempotency key, but exactly-once business effect requires
// the payment handler to persist and enforce that key with a unique constraint or InboxMessage.
public sealed record PayOrderCommand(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string PaymentMethod,
    string IdempotencyKey) : IRequest<PayOrderResponse>, IIdempotentCommand;
