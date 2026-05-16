using FluentValidation;

namespace ECommerce.Domain.Service.Payment.PayOrder;

public sealed class PayOrderCommandValidator : AbstractValidator<PayOrderCommand>
{
    public PayOrderCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.Amount).GreaterThan(0);
        RuleFor(command => command.PaymentMethod).NotEmpty().MaximumLength(100);
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(200);
    }
}
