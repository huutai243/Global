using ECommerce.Infrastructure.Observability;

namespace ECommerce.Ordering.WebApi.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseOrderingWebApi(this WebApplication app)
    {
        app.UseCorrelationId();
        app.UseGlobalExceptionHandling();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health");
        app.MapControllers();

        return app;
    }
}