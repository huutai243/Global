using ECommerce.Catalog.Worker.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCatalogWorkerServices(builder.Configuration);

var host = builder.Build();

await host.RunAsync();