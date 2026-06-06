using MediatR;

namespace ECommerce.Catalog.Application.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid CategoryId) : IRequest;
