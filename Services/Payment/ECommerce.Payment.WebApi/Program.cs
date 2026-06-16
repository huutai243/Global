using ECommerce.Payment.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPaymentWebApiServices(builder.Configuration);

var app = builder.Build();

app.UsePaymentWebApi();

app.Run();