using ECommerce.Payment.Infrastructure;
using ECommerce.Payment.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddPaymentInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
