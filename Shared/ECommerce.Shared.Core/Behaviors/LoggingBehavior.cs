using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Shared.Core.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling request {RequestName}", requestName);

        var response = await next();

        logger.LogInformation("Handled request {RequestName}", requestName);

        return response;
    }
}