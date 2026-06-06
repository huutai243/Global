using ECommerce.Infrastructure.Observability;

namespace ECommerce.Catalog.WebApi.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseCatalogWebApp(this WebApplication app)
    {
        app.UseCorrelationId();
        app.UseGlobalExceptionHandling();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors("Frontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health");
        app.MapControllers();

        return app;
    }
}