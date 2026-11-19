using System.Security.Cryptography;

namespace VisaUchet.Domain;

// SEC-01: passwords are stored only as PBKDF2 hashes (format PBKDF2$iterations$salt$hash)
public static class PasswordHasher
{
    public const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const string Scheme = "PBKDF2";
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);
        return $"{Scheme}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || !TryParse(storedHash, out var iterations, out var salt, out var expected))
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    // A damaged hash in the database must not crash the login form
    private static bool TryParse(string? storedHash, out int iterations, out byte[] salt, out byte[] hash)
    {
        iterations = 0;
        salt = hash = [];
        var parts = storedHash?.Split('$') ?? [];
        return parts.Length == 4 && parts[0] == Scheme
            && int.TryParse(parts[1], out iterations) && iterations > 0
            && TryDecode(parts[2], out salt) && TryDecode(parts[3], out hash) && hash.Length > 0;
    }

    private static bool TryDecode(string text, out byte[] bytes)
    {
        var buffer = new byte[text.Length];
        var ok = Convert.TryFromBase64String(text, buffer, out var written);
        bytes = ok ? buffer[..written] : [];
        return ok;
    }
}
