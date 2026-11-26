namespace Monei.Core.Entities;

using System.Security.Cryptography;

public sealed class Account
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    // Base64(salt + derived hash)
    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int CurrencyId { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastChangeDate { get; set; }

    public Guid Guid { get; set; }

    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 10000;

    public static string HashPassword(string password, byte[]? salt = null)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        if (salt is not null && salt.Length != SaltSize)
            throw new ArgumentException($"Salt must be {SaltSize} bytes", nameof(salt));

        if (salt is null || salt.Length == 0)
        {
            salt = new byte[SaltSize];
            RandomNumberGenerator.Fill(salt);
        }
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        // concatenate salt + hash
        var saltAndHash = new byte[salt.Length + HashSize];
        Array.Copy(salt, 0, saltAndHash, 0, SaltSize);
        Array.Copy(hash, 0, saltAndHash, SaltSize, HashSize);

        return Convert.ToBase64String(saltAndHash);
    }

    public bool VerifyPassword(string password)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(Password))
            return false;

        byte[] saltAndHash;
        try
        {
            saltAndHash = Convert.FromBase64String(Password);
        }
        catch (FormatException)
        {
            return false;
        }

        if (saltAndHash.Length < SaltSize + HashSize)
            return false;

        var salt = new byte[SaltSize];
        var hash = new byte[HashSize];
        Array.Copy(saltAndHash, 0, salt, 0, SaltSize);
        Array.Copy(saltAndHash, SaltSize, hash, 0, HashSize);

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return CryptographicOperations.FixedTimeEquals(computedHash, hash);
    }
}
