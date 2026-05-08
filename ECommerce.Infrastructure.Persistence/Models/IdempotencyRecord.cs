namespace ECommerce.Infrastructure.Persistence.Models;

public class IdempotencyRecord
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string RequestHash { get; set; } = string.Empty;

    public string? ResponsePayload { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
