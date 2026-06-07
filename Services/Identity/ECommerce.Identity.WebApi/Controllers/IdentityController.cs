using ECommerce.Identity.Application.ForgotPassword;
using ECommerce.Identity.Application.Login;
using ECommerce.Identity.Application.Profile;
using ECommerce.Identity.Application.Register;
using ECommerce.Identity.Application.ResetPassword;
using ECommerce.Identity.Application.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Identity.WebApi.Controllers;

[ApiController]
[Route("api/identity")]
public sealed class IdentityController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> RegisterAsync([FromBody] RegisterCustomerCommand command, CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> LoginAsync([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfileAsync(CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetProfileQuery(), cancellationToken);

        return Ok(response);
    }

    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfileAsync([FromBody] UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);

        return Ok(new
        {
            message = "If the email exists, a password reset link has been sent."
        });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);

        return Ok(new
        {
            message = "Password has been reset successfully."
        });
    }
}