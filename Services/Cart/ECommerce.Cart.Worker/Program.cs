using ECommerce.Cart.Infrastructure;
using ECommerce.Cart.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddCartInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
