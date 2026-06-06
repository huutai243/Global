using ECommerce.Catalog.Domain.Responses;
using MediatR;

namespace ECommerce.Catalog.Application.GetCategories;

public sealed record GetCategoriesQuery(bool IncludeInactive) : IRequest<IReadOnlyCollection<CategoryResponse>>;
