using ECommerce.Inventory.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInventoryWebApiServices(builder.Configuration);

var app = builder.Build();

app.UseInventoryWebApi();

app.Run();