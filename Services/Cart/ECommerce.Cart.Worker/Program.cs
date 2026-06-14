using ECommerce.Cart.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddCartInfrastructure(builder.Configuration);

var host = builder.Build();
host.Run();
