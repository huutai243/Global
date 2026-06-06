using ECommerce.Catalog.Domain.Responses;
using ECommerce.Infrastructure.Storage;
using MediatR;

namespace ECommerce.Catalog.Application.CreateCategory;

public sealed record CreateCategoryCommand(string Name, string? Description, FileUploadRequest? Image) : IRequest<CategoryResponse>;
