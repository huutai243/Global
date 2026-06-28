using ECommerce.Payment.Worker.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPaymentWorkerServices(builder.Configuration);

var host = builder.Build();

host.Run();