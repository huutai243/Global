using ECommerce.Analytics.Infrastructure;
using ECommerce.Analytics.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAnalyticsInfrastructure();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
