using ECommerce.Payment.Infrastructure;
using ECommerce.Payment.Worker;
using ECommerce.Payment.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddPaymentInfrastructure();
builder.Services.AddScoped<CreatePaymentCommandConsumer>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
