using ECommerce.Catalog.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCatalogApplicationServices(builder.Configuration);

var app = builder.Build();

app.UseCatalogWebApp();

await app.RunAsync();