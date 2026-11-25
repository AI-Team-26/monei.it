namespace Monei.Core.Entities;

using System.Security.Cryptography;

public sealed class Account
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Stored credential for this account. Not plain text: holds Base64(salt + derived hash).
    /// The salt is kept together with the hash on purpose - it must be known to check a login.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int CurrencyId { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastChangeDate { get; set; }

    public Guid Guid { get; set; }

    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 10000;

    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        var saltAndHash = new byte[SaltSize + HashSize];
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
