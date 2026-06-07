using FluentValidation;

namespace ECommerce.Ordering.Application.CheckoutCart;

public sealed class CheckoutCartCommandValidator : AbstractValidator<CheckoutCartCommand>
{
    public CheckoutCartCommandValidator()
    {
        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(100);
    }
}
