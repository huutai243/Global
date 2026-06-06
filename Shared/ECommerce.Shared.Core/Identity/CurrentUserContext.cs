using ECommerce.Shared.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ECommerce.Shared.Core.Identity;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    public Guid? CustomerId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User
                .FindFirst("customer_id")
                ?.Value;

            return Guid.TryParse(value, out var customerId)
                ? customerId
                : null;
        }
    }

    public Guid? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)
                ?.Value
                ?? httpContextAccessor.HttpContext?.User
                    .FindFirst("sub")
                    ?.Value;

            return Guid.TryParse(value, out var userId)
                ? userId
                : null;
        }
    }

    public string? Email => httpContextAccessor.HttpContext?.User
        .FindFirst(ClaimTypes.Email)
        ?.Value;

    public string? Role => httpContextAccessor.HttpContext?.User
        .FindFirst(ClaimTypes.Role)
        ?.Value;

    public bool IsAdmin => string.Equals(
        Role,
        "Admin",
        StringComparison.OrdinalIgnoreCase);
}