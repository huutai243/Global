using System.Security.Cryptography;
using System.Text;
using ECommerce.Infrastructure.Security.Core;

namespace ECommerce.Infrastructure.Security;

public sealed class PasswordResetTokenService : IPasswordResetTokenService
{
    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", string.Empty);
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(bytes);
    }
}