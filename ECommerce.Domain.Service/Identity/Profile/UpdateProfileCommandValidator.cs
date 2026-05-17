using FluentValidation;

namespace ECommerce.Domain.Service.Identity.Profile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(command => command.FullName)
            .NotEmpty()
            .MaximumLength(200);
    }
}