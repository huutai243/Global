namespace ECommerce.Core.SharedLibs.Interfaces;

public interface ICurrentUserContext
{
    Guid? UserId { get; }

    Guid? CustomerId { get; }

    string? Role { get; }

    bool IsAdmin { get; }
}
