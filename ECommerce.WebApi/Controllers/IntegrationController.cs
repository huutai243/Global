using ECommerce.Core.SharedLibs.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.WebApi.Controllers;

[ApiController]
[Route("api/admin/integrations")]
[Authorize(Roles = "Admin")]
public class IntegrationController(IKafkaProducer kafkaProducer) : ControllerBase
{
    [HttpPost("kafka/test")]
    public async Task<IActionResult> ProduceKafkaTestAsync(CancellationToken cancellationToken)
    {
        await kafkaProducer.ProduceAsync("ecommerce-events", new { Message = "Kafka producer skeleton", OccurredAt = DateTime.UtcNow }, cancellationToken);
        return Accepted();
    }
}
