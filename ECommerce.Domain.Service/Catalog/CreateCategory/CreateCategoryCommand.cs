using ECommerce.Domain.Core.Catalog.Responses;
using MediatR;

namespace ECommerce.Domain.Service.Catalog.CreateCategory;

public sealed record CreateCategoryCommand(string Name, string? Description) : IRequest<CategoryResponse>;
