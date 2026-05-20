using FluentValidation;

namespace ECommerce.Domain.Service.Catalog.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).MaximumLength(1000);
    }
}
