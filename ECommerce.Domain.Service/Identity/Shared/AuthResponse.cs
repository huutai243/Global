namespace ECommerce.Domain.Service.Identity.Shared;

public sealed record AuthResponse(Guid UserId, Guid? CustomerId, string Email, string Role, string AccessToken);
