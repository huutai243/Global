using ECommerce.Catalog.Domain.Responses;
using ECommerce.Infrastructure.Storage;
using MediatR;

namespace ECommerce.Catalog.Application.UpdateCategory;

public sealed record UpdateCategoryCommand(Guid CategoryId, string Name, string? Description, bool IsActive, FileUploadRequest? Image) : IRequest<CategoryResponse>;
