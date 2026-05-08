using System.Net;
using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Core.SharedLibs.Responses;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Observability;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode, details) = exception switch
        {
            ValidationException validationException => (HttpStatusCode.BadRequest, "validation_error", validationException.Errors.Select(error => error.ErrorMessage).ToArray()),
            NotFoundException => (HttpStatusCode.NotFound, "not_found", Array.Empty<string>()),
            BusinessRuleException => (HttpStatusCode.BadRequest, "business_rule_error", Array.Empty<string>()),
            ForbiddenAccessException => (HttpStatusCode.Forbidden, "forbidden", Array.Empty<string>()),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "unauthorized", Array.Empty<string>()),
            ConcurrencyException => (HttpStatusCode.Conflict, "concurrency_conflict", Array.Empty<string>()),
            IdempotencyConflictException => (HttpStatusCode.Conflict, "idempotency_conflict", Array.Empty<string>()),
            _ => (HttpStatusCode.InternalServerError, "server_error", Array.Empty<string>())
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            logger.LogWarning(exception, "Handled exception {ErrorCode}", errorCode);
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var value)
            ? value?.ToString() ?? string.Empty
            : string.Empty;

        await context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            ErrorCode = errorCode,
            Message = exception.Message,
            CorrelationId = correlationId,
            Details = details
        });
    }
}
