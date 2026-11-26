using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Monei.Core.Entities;

public sealed class Account
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    // Argon2id PHC string ($argon2id$v=19$m=...,t=...,p=...$salt$hash); never plain text.
    public string Password { get; private set; } = string.Empty;

    public bool PasswordResetRequired { get; set; }

    public string Role { get; set; } = string.Empty;

    public int CurrencyId { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastChangeDate { get; set; }

    public Guid Guid { get; set; }

    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int PhcVersion = 19;
    // Above the OWASP minimum for Argon2id (19 MiB / 2 iterations).
    private const int MemoryKiB = 64 * 1024;
    private const int Iterations = 3;
    private const int Parallelism = 1;

    // Bounds for parameters read back from a stored PHC string. A corrupted or hostile row must
    // fail verification, never make Argon2 allocate terabytes or throw.
    private const int MinMemoryKiB = 8 * 1024;
    private const int MaxMemoryKiB = 256 * 1024;
    private const int MaxIterations = 10;
    private const int MaxParallelism = 8;

    public void SetPassword(string plainText) => Password = HashPassword(plainText);

    public static string HashPassword(string password, byte[]? salt = null)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        // null means "generate a fresh random salt"; any provided salt must be exactly SaltSize bytes.
        if (salt is null)
        {
            salt = new byte[SaltSize];
            RandomNumberGenerator.Fill(salt);
        }
        else if (salt.Length != SaltSize)
        {
            throw new ArgumentException($"Salt must be {SaltSize} bytes", nameof(salt));
        }

        var hash = Derive(password, salt, MemoryKiB, Iterations, Parallelism);
        return ToPhcString(MemoryKiB, Iterations, Parallelism, salt, hash);
    }

    public bool VerifyPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        // The stored string carries its own parameters, so old hashes keep verifying
        // even after MemoryKiB/Iterations are raised here.
        if (ParsePhcString(Password) is not (var memory, var iterations, var parallelism, var salt, var storedHash))
            return false;

        var computed = Derive(password, salt, memory, iterations, parallelism);
        return CryptographicOperations.FixedTimeEquals(computed, storedHash);
    }

    private static byte[] Derive(string password, byte[] salt, int memoryKiB, int iterations, int parallelism)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        try
        {
            using var argon = new Argon2id(passwordBytes)
            {
                Salt = salt,
                MemorySize = memoryKiB,
                Iterations = iterations,
                DegreeOfParallelism = parallelism
            };
            return argon.GetBytes(HashSize);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    private static string ToPhcString(int memoryKiB, int iterations, int parallelism, byte[] salt, byte[] hash)
        => $"$argon2id$v={PhcVersion}$m={memoryKiB},t={iterations},p={parallelism}${Base64UrlEncode(salt)}${Base64UrlEncode(hash)}";

    private static (int Memory, int Iterations, int Parallelism, byte[] Salt, byte[] Hash)? ParsePhcString(string phc)
    {
        // $argon2id$v=19$m=<mem>,t=<iter>,p=<par>$<b64url salt>$<b64url hash>
        var parts = phc.Split('$');
        if (parts.Length != 6 || parts[0] != "" || parts[1] != "argon2id" || parts[2] != $"v={PhcVersion}")
            return null;

        var params_ = parts[3].Split(',');
        if (params_.Length != 3
            || !TryReadParam(params_[0], 'm', out var memory)
            || !TryReadParam(params_[1], 't', out var iterations)
            || !TryReadParam(params_[2], 'p', out var parallelism))
            return null;

        var salt = Base64UrlDecode(parts[4]);
        var hash = Base64UrlDecode(parts[5]);
        if (salt is null || hash is null)
            return null;

        // Sizes and parameter bounds are checked before the values reach Argon2: an out-of-range
        // m would otherwise request gigabytes of memory per login attempt.
        if (salt.Length != SaltSize || hash.Length != HashSize
            || memory < MinMemoryKiB || memory > MaxMemoryKiB
            || iterations > MaxIterations
            || parallelism > MaxParallelism)
            return null;

        return (memory, iterations, parallelism, salt, hash);
    }

    private static bool TryReadParam(string text, char expectedPrefix, out int value)
    {
        value = 0;
        var separator = text.IndexOf('=');
        return separator == 1 && text[0] == expectedPrefix && int.TryParse(text[(separator + 1)..], out value) && value > 0;
    }

    private static string Base64UrlEncode(byte[] data) => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[]? Base64UrlDecode(string text)
    {
        try
        {
            var base64 = text.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
                case 1: return null;
            }
            return Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
