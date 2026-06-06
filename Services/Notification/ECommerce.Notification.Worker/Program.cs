using ECommerce.Notification.Infrastructure;
using ECommerce.Notification.Worker;
using ECommerce.Notification.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddNotificationInfrastructure();
builder.Services.AddScoped<OrderPaidEventConsumer>();
builder.Services.AddScoped<OrderCancelledEventConsumer>();
builder.Services.AddScoped<PaymentSucceededEventConsumer>();
builder.Services.AddScoped<PaymentFailedEventConsumer>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
