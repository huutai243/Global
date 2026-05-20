using MediatR;

namespace ECommerce.Domain.Service.Catalog.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid CategoryId) : IRequest;
