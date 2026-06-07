using ECommerce.Catalog.Infrastructure;
using ECommerce.Catalog.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddCatalogInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
