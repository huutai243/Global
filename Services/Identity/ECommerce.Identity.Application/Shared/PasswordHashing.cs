using System.Security.Cryptography;
using System.Text;

namespace ECommerce.Identity.Application.Shared;

internal static class PasswordHashing
{
    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
