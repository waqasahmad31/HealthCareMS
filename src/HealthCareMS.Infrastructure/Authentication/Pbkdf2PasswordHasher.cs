using System.Security.Cryptography;
using HealthCareMS.Application.Abstractions.Authentication;

namespace HealthCareMS.Infrastructure.Authentication;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 210_000;
    private const string Prefix = "HCMSPBKDF2";

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        return string.Join('$', Prefix, "1", Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split('$');
        if (parts.Length != 5 || parts[0] != Prefix || !int.TryParse(parts[2], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[3]);
        var expectedHash = Convert.FromBase64String(parts[4]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
