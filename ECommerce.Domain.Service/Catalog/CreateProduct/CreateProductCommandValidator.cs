using FluentValidation;

namespace ECommerce.Domain.Service.Catalog.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Price).GreaterThan(0);
        RuleFor(command => command.InitialStock).GreaterThanOrEqualTo(0);
    }
}
