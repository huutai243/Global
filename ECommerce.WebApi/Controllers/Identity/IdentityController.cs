using ECommerce.Domain.Service.Identity.Login;
using ECommerce.Domain.Service.Identity.Profile;
using ECommerce.Domain.Service.Identity.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.WebApi.Controllers.Identity;

[ApiController]
[Route("api/identity")]
public class IdentityController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterAsync(RegisterCustomerCommand command, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(command, cancellationToken));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(command, cancellationToken));
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfileAsync(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetProfileQuery(), cancellationToken));
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfileAsync(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(command, cancellationToken));
    }
}
