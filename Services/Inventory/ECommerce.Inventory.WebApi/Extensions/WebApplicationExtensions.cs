using ECommerce.Shared.Observability;

namespace ECommerce.Inventory.WebApi.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseInventoryWebApi(this WebApplication app)
    {
        app
            .UseCrossCuttingMiddleware()
            .UseDevelopmentTools()
            .UseSecurityMiddleware()
            .UseInventoryEndpoints();

        return app;
    }

    private static WebApplication UseCrossCuttingMiddleware(this WebApplication app)
    {
        app.UseCorrelationId();
        app.UseGlobalExceptionHandling();

        return app;
    }

    private static WebApplication UseDevelopmentTools(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.UseSwagger();
        app.UseSwaggerUI();

        return app;
    }

    private static WebApplication UseSecurityMiddleware(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    private static WebApplication UseInventoryEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapControllers();

        return app;
    }
}