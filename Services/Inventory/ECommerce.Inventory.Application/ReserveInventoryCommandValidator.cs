using ECommerce.Shared.Contracts;
using FluentValidation;

namespace ECommerce.Inventory.Application.ReserveInventory;

public sealed class ReserveInventoryCommandValidator : AbstractValidator<ReserveInventoryCommand>
{
    public ReserveInventoryCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty();

        RuleFor(x => x.CustomerId)
            .NotEmpty();

        RuleFor(x => x.Items)
            .NotEmpty();

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.ProductId)
                    .NotEmpty();

                item.RuleFor(x => x.ProductName)
                    .NotEmpty()
                    .MaximumLength(300);

                item.RuleFor(x => x.Quantity)
                    .GreaterThan(0);
            });
    }
}