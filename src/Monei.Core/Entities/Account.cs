namespace Monei.Core.Entities;

using System.Security.Cryptography;

public sealed class Account
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Hashed password using PBKDF2 with HMAC-SHA256.
    /// Format: Base64(salt(16 bytes) + hash(32 bytes)).
    /// The salt is stored in the open alongside the hash - this is normal practice.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int CurrencyId { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastChangeDate { get; set; }

    public Guid Guid { get; set; }

    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int Iterations = 10000; // configurable work factor

    /// <summary>
    /// Hashes a plain text password using PBKDF2 with HMAC-SHA256.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <returns>Base64-encoded salt + hash (salt stored in the open, which is normal practice).</returns>
    /// <exception name="ArgumentException">Thrown when password is null or empty.</exception>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        // Generate 16-byte salt using RandomNumberGenerator
        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

#pragma warning disable SYSLIB0060 // Type is obsolete
        // Use 4-parameter constructor with HashAlgorithmName (obsolete API, but functional in .NET 10)
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);
#pragma warning restore SYSLIB0060

        // Combine salt and hash, then Base64 encode
        var saltAndHash = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, saltAndHash, 0, SaltSize);
        Array.Copy(hash, 0, saltAndHash, SaltSize, HashSize);

        return Convert.ToBase64String(saltAndHash);
    }

    /// <summary>
    /// Verifies if the provided plain text password matches the stored hash.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <returns>True if the password matches, false otherwise.</returns>
    public bool VerifyPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        if (string.IsNullOrEmpty(Password))
        {
            return false;
        }

        // Decode the Base64 string to retrieve salt and hash
        byte[] saltAndHash;
        try
        {
            saltAndHash = Convert.FromBase64String(Password);
        }
        catch
        {
            return false;
        }

        // Verify minimum length: salt(16) + hash(32) = 48 bytes minimum
        if (saltAndHash.Length < SaltSize + HashSize)
        {
            return false;
        }

        // Extract salt and stored hash
        var salt = new byte[SaltSize];
        var hash = new byte[HashSize];
        Array.Copy(saltAndHash, 0, salt, 0, SaltSize);
        Array.Copy(saltAndHash, SaltSize, hash, 0, HashSize);

#pragma warning disable SYSLIB0060 // Type is obsolete
        // Use 4-parameter constructor with HashAlgorithmName to derive hash from provided password
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var computedHash = pbkdf2.GetBytes(HashSize);
#pragma warning restore SYSLIB0060

        // Compare computed hash with stored hash
        return CryptoHelper.ByteArraysAreEqual(computedHash, hash);
    }
}

/// <summary>
/// Helper class for secure byte array comparison (prevents timing attacks).
/// </summary>
internal static class CryptoHelper
{
    /// <summary>
    /// Compares two byte arrays in constant time to prevent timing attacks.
    /// </summary>
    /// <param name="a">First byte array.</param>
    /// <param name="b">Second byte array.</param>
    /// <returns>True if arrays are equal, false otherwise.</returns>
    public static bool ByteArraysAreEqual(byte[] a, byte[] b)
    {
        if (a is null || b is null)
        {
            return false;
        }

        if (a.Length != b.Length)
        {
            return false;
        }

        int result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}
