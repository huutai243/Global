using ECommerce.Domain.Core.Catalog.Responses;
using ECommerce.Infrastructure.Storage;
using MediatR;

namespace ECommerce.Domain.Service.Catalog.CreateCategory;

public sealed record CreateCategoryCommand(string Name, string? Description, FileUploadRequest? Image) : IRequest<CategoryResponse>;
