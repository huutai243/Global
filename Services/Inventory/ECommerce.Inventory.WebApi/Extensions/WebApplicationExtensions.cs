using ECommerce.Shared.Observability;

namespace ECommerce.Inventory.WebApi.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseInventoryWebApi(this WebApplication app)
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