using FluentValidation;

namespace ECommerce.Catalog.Application.SearchProducts;

public sealed class SearchProductsQueryValidator
    : AbstractValidator<SearchProductsQuery>
{
    private const int MaxKeywordLength = 100;
    private const int MaxPageSize = 50;

    public SearchProductsQueryValidator()
    {
        RuleFor(query => query.Keyword)
            .MaximumLength(MaxKeywordLength);

        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, MaxPageSize);

        RuleFor(query => query.MinPrice)
            .GreaterThanOrEqualTo(0)
            .When(query => query.MinPrice is not null);

        RuleFor(query => query.MaxPrice)
            .GreaterThanOrEqualTo(0)
            .When(query => query.MaxPrice is not null);

        RuleFor(query => query)
            .Must(query =>
                query.MinPrice is null ||
                query.MaxPrice is null ||
                query.MinPrice <= query.MaxPrice)
            .WithMessage("MinPrice must be less than or equal to MaxPrice.");
    }
}