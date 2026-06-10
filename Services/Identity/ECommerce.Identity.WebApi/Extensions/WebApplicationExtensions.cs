using ECommerce.Shared.Observability;

namespace ECommerce.Identity.WebApi.Infras.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseIdentityWebApp(this WebApplication app)
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