using ECommerce.Ordering.Worker.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOrderingWorkerServices(builder.Configuration);

var host = builder.Build();

await host.RunAsync();