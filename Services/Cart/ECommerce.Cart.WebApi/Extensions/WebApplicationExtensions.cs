using ECommerce.Shared.Observability;

namespace ECommerce.Cart.WebApi.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseCartWebApp(this WebApplication app)
    {
        app
            .UseCrossCuttingMiddleware()
            .UseDevelopmentTools()
            .UseFrontendCors()
            .UseSecurityMiddleware()
            .UseCartEndpoints();

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

    private static WebApplication UseFrontendCors(this WebApplication app)
    {
        app.UseCors("Frontend");

        return app;
    }

    private static WebApplication UseSecurityMiddleware(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    private static WebApplication UseCartEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapControllers();

        return app;
    }
}