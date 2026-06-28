using ECommerce.Payment.Application.PayOrder;
using ECommerce.Shared.Core.Interfaces;
using MediatR;

namespace ECommerce.Payment.Application.InitiatePayment;

// IDEMPOTENCY NOTE:
// This command is for internal Payment API/MediatR usage.
// Integration messages from Ordering must use ECommerce.Shared.Contracts.Payment.PayOrderCommand.
public sealed record InitiatePaymentCommand(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string IdempotencyKey) : IRequest<InitiatePaymentResponse>, IIdempotentCommand;