using ECommerce.Domain.Core.Catalog.Responses;
using MediatR;

namespace ECommerce.Domain.Service.Catalog.GetCategories;

public sealed record GetCategoriesQuery(bool IncludeInactive) : IRequest<IReadOnlyCollection<CategoryResponse>>;
