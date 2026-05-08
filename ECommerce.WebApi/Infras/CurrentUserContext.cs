using System.Security.Claims;
using ECommerce.Core.SharedLibs.Interfaces;
using ECommerce.Identity.Core.Models;

namespace ECommerce.WebApi.Infras;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    public Guid? UserId => TryReadGuid(ClaimTypes.NameIdentifier);

    public Guid? CustomerId => TryReadGuid("customer_id");

    public string? Role => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);

    public bool IsAdmin => Role == UserRoles.Admin;

    private Guid? TryReadGuid(string claimType)
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
        return Guid.TryParse(value, out var parsedValue) ? parsedValue : null;
    }
}
