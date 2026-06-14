using ECommerce.Cart.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCartApplicationServices(builder.Configuration);

var app = builder.Build();

app.UseCartWebApp();

await app.RunAsync();