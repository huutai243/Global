using ECommerce.Ordering.Infrastructure;
using ECommerce.Ordering.Worker;
using ECommerce.Ordering.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddOrderingInfrastructure();
builder.Services.AddScoped<InventoryReservedEventConsumer>();
builder.Services.AddScoped<InventoryReserveFailedEventConsumer>();
builder.Services.AddScoped<PaymentSucceededEventConsumer>();
builder.Services.AddScoped<PaymentFailedEventConsumer>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
