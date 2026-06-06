using ECommerce.Inventory.Infrastructure;
using ECommerce.Inventory.Worker;
using ECommerce.Inventory.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInventoryInfrastructure();
builder.Services.AddScoped<ReserveInventoryCommandConsumer>();
builder.Services.AddScoped<ConfirmInventoryReservationCommandConsumer>();
builder.Services.AddScoped<ReleaseInventoryReservationCommandConsumer>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
