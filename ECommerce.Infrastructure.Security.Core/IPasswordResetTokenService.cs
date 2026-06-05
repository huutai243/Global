namespace ECommerce.Infrastructure.Security.Core;

public interface IPasswordResetTokenService
{
    string GenerateToken();

    string HashToken(string token);
}