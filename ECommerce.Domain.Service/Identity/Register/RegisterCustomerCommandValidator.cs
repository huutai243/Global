using FluentValidation;

namespace ECommerce.Domain.Service.Identity.Register;

public sealed class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress();
        RuleFor(command => command.Password).MinimumLength(8);
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(200);
    }
}
