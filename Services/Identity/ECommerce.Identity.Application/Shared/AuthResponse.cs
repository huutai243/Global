namespace ECommerce.Identity.Application.Shared;

public sealed record AuthResponse(Guid UserId, Guid? CustomerId, string Email, string Role, string AccessToken);
