using ECommerce.Identity.WebApi.Infras.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityApplicationServices(builder.Configuration);

var app = builder.Build();

app.UseIdentityWebApp();

await app.RunAsync();