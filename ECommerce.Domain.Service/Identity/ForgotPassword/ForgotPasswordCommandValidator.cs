using FluentValidation;

namespace ECommerce.Domain.Service.Identity.ForgotPassword
{
    public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordCommandValidator()
        {
            RuleFor(command => command.Email)
                .NotEmpty()
                .EmailAddress();
        }
    }
}
