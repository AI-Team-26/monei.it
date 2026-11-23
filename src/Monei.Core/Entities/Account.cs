namespace Monei.Core.Entities;

using System.Security.Cryptography;
using System.Text;

public sealed class Account
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Hashed password stored as Base64-encoded SHA256 value.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int CurrencyId { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastChangeDate { get; set; }

    public Guid Guid { get; set; }

    /// <summary>
    /// Hashes a plain text password using SHA256 and returns a Base64-encoded string.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <returns>Base64-encoded SHA256 hash of the password.</returns>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return string.Empty;
        }

        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
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

        var expectedHash = HashPassword(password);
        return string.Equals(Password, expectedHash, StringComparison.OrdinalIgnoreCase);
    }
}
