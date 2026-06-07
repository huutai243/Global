using ECommerce.Ordering.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOrderingWebApiServices(builder.Configuration);

var app = builder.Build();

app.UseOrderingWebApi();

app.Run();