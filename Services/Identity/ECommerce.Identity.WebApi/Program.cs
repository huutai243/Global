using ECommerce.Identity.WebApi.Infras.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddIdentityApplicationServices(builder.Configuration);

var app = builder.Build();

app.UseIdentityWebApp();

app.Run();