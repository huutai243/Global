using ECommerce.Payment.Application.InitiatePayment;
using ECommerce.Shared.Contracts.Payment;
using FluentValidation;

namespace ECommerce.Payment.Application.PayOrder;

public sealed class PayOrderCommandValidator : AbstractValidator<InitiatePaymentCommand>
{
    public PayOrderCommandValidator()
    {
        RuleFor(command => command.OrderId)
            .NotEmpty();

        RuleFor(command => command.CustomerId)
            .NotEmpty();

        RuleFor(command => command.Amount)
            .GreaterThan(0);

        RuleFor(command => command.Currency)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(200);
    }
}