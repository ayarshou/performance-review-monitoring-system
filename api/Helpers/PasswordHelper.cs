using System.Security.Cryptography;
using System.Text;

namespace PerformanceReviewApi.Helpers;

/// <summary>PBKDF2-SHA256 password helper — no extra NuGet packages required.</summary>
public static class PasswordHelper
{
    private const int Iterations = 10_000;
    private const int HashSize   = 32;
    private const int SaltSize   = 16;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;
        var salt     = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual   = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
