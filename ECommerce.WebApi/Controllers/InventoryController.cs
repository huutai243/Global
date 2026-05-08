using ECommerce.Inventory.Service.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.WebApi.Controllers;

[ApiController]
[Route("api/admin/inventory")]
[Authorize(Roles = "Admin")]
public class InventoryController(ISender sender) : ControllerBase
{
    [HttpPost("adjust")]
    public async Task<IActionResult> AdjustInventoryAsync(AdjustInventoryCommand command, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(command, cancellationToken));
    }
}
