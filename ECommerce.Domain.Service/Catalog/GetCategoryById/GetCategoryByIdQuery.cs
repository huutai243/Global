using ECommerce.Domain.Core.Catalog.Responses;
using MediatR;

namespace ECommerce.Domain.Service.Catalog.GetCategoryById;

public sealed record GetCategoryByIdQuery(Guid CategoryId) : IRequest<CategoryResponse>;
