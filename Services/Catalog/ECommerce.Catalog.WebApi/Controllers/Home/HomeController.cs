using ECommerce.Catalog.Application.Home;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Catalog.WebApi.Controllers;

[ApiController]
[Route("api/home")]
public sealed class HomeController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetHomeAsync(CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetHomeQuery(), cancellationToken);

        return Ok(response);
    }
}
