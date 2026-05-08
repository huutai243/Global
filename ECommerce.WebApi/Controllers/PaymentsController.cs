using ECommerce.Payment.Service.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.WebApi.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Roles = "Customer,Admin")]
public class PaymentsController(ISender sender) : ControllerBase
{
    [HttpPost("pay-order")]
    public async Task<IActionResult> PayOrderAsync(PayOrderCommand command, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(command, cancellationToken));
    }
}
