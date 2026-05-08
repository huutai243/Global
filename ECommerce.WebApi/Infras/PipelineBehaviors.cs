using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Core.SharedLibs.Interfaces;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.WebApi.Infras;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(validator => validator.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .ToArray();

        if (failures.Length > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling request {RequestName}", typeof(TRequest).Name);
        var response = await next();
        logger.LogInformation("Handled request {RequestName}", typeof(TRequest).Name);
        return response;
    }
}

public sealed class IdempotencyBehavior<TRequest, TResponse>(ECommerceDbContext dbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IIdempotentCommand idempotentCommand)
        {
            return await next();
        }

        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))));
        var existingRecord = await dbContext.IdempotencyRecords
            .FirstOrDefaultAsync(record => record.Key == idempotentCommand.IdempotencyKey, cancellationToken);

        if (existingRecord is not null)
        {
            throw new IdempotencyConflictException("A request with the same idempotency key was already processed or is in progress.");
        }

        dbContext.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Key = idempotentCommand.IdempotencyKey,
            RequestHash = requestHash,
            Status = "Processing",
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await next();

        var record = await dbContext.IdempotencyRecords
            .FirstAsync(item => item.Key == idempotentCommand.IdempotencyKey, cancellationToken);
        record.Status = "Completed";
        record.ResponsePayload = JsonSerializer.Serialize(response);
        record.CompletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }
}
