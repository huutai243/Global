using ECommerce.Inventory.Worker.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInventoryWorkerServices(builder.Configuration);

var host = builder.Build();

await host.RunAsync();