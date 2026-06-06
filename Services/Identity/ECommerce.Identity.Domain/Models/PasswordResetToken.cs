namespace ECommerce.Identity.Domain.Models;

public sealed class PasswordResetToken
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
}