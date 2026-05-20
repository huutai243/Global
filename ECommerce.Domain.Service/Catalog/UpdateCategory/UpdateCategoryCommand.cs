using ECommerce.Domain.Core.Catalog.Responses;
using MediatR;

namespace ECommerce.Domain.Service.Catalog.UpdateCategory;

public sealed record UpdateCategoryCommand(Guid CategoryId, string Name, string? Description, bool IsActive) : IRequest<CategoryResponse>;
