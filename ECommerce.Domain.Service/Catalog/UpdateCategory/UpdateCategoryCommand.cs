using ECommerce.Domain.Core.Catalog.Responses;
using ECommerce.Infrastructure.Storage;
using MediatR;

namespace ECommerce.Domain.Service.Catalog.UpdateCategory;

public sealed record UpdateCategoryCommand(Guid CategoryId, string Name, string? Description, bool IsActive, FileUploadRequest? Image) : IRequest<CategoryResponse>;
