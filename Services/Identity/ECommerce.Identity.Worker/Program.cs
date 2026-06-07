using ECommerce.Identity.Infrastructure;
using ECommerce.Identity.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
