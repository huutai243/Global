using ECommerce.Infrastructure.BackgroundJobs;
using ECommerce.Infrastructure.Persistence.Extensions;
using ECommerce.Infrastructure.RabbitMq;
using ECommerce.WorkerWebApp;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddBackgroundJobs(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
