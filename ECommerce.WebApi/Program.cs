using ECommerce.WebApi.Infras.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddRepositories(builder.Configuration);

var app = builder.Build();

app.UseWebApp();

app.Run();
