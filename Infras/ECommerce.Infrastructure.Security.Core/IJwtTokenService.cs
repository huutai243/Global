namespace ECommerce.Infrastructure.Security.Core;

public interface IJwtTokenService
{
    string CreateToken(Guid userId, Guid? customerId, string email, string role);
}
