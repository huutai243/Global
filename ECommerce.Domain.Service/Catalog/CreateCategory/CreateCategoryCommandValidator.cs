using FluentValidation;

namespace ECommerce.Domain.Service.Catalog.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).MaximumLength(1000);
    }
}
