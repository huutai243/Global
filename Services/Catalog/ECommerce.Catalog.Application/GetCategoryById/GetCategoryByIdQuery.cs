using ECommerce.Catalog.Domain.Responses;
using MediatR;

namespace ECommerce.Catalog.Application.GetCategoryById;

public sealed record GetCategoryByIdQuery(Guid CategoryId) : IRequest<CategoryResponse>;
