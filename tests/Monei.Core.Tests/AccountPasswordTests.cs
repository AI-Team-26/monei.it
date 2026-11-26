using Monei.Core.Entities;
using NUnit.Framework;

namespace Monei.Core.Tests;

[TestFixture]
public class AccountPasswordTests
{
    private static readonly byte[] FixedSalt = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();

    // Pre-calculated once for password "MySecret123!" and FixedSalt with m=65536 KiB, t=3, p=1.
    // If anyone changes the algorithm or parameters this fails instead of silently staying green.
    private const string KnownHash = "$argon2id$v=19$m=65536,t=3,p=1$AAECAwQFBgcICQoLDA0ODw$FMb_ouevQlnH2jJvzAsnk4N_eGA4t01gvz5iEPb2ktw";

    [Test]
    public void HashPassword_MatchesKnownValue_WithFixedSalt()
    {
        var hash = Account.HashPassword("MySecret123!", FixedSalt);

        Assert.That(hash, Is.EqualTo(KnownHash));
    }

    [Test]
    public void VerifyPassword_ReturnsFalse_NeverThrows_ForMalformedStoredValues()
    {
        // Every branch of ParsePhcString must fail closed (return false), never throw,
        // and never reach Argon2 with hostile parameters.
        var malformed = new[]
        {
            KnownHash.Replace("argon2id", "argon2i"),                     // wrong variant
            KnownHash[..^10],                                             // truncated hash part
            $"{KnownHash}$extra",                                         // too many segments
            KnownHash.Replace("FMb_", "!!!"),                             // invalid base64 characters
            KnownHash.Replace("m=65536", "m=2000000000"),                // out-of-range memory parameter
            "$argon2id$v=19$m=65536,t=3,p=1$AAAA$FMb_ouevQlnH2jJvzAsnk4N_eGA4t01gvz5iEPb2ktw" // 4-byte salt
        };

        foreach (var stored in malformed)
        {
            var account = new Account();
            StoreRawPassword(account, stored);
            Assert.That(account.VerifyPassword("MySecret123!"), Is.False);
        }
    }

    [Test]
    public void HashPassword_Throws_ForNullOrEmptyPassword()
    {
        Assert.Throws<ArgumentException>(() => Account.HashPassword(null!));
        Assert.Throws<ArgumentException>(() => Account.HashPassword(string.Empty));
    }

    [Test]
    public void SetPassword_AllowsVerifyPassword()
    {
        var account = new Account();
        account.SetPassword("MySecret123!");

        Assert.That(account.VerifyPassword("MySecret123!"), Is.True);
        Assert.That(account.VerifyPassword("Wrong123!"), Is.False);
    }

    [Test]
    public void VerifyPassword_ReturnsFalse_ForLegacyPlainTextStoredValue()
    {
        var account = new Account();
        StoreRawPassword(account, "secret");

        Assert.That(account.VerifyPassword("secret"), Is.False);
    }

    [Test]
    public void VerifyPassword_ReturnsFalse_ForEmptyStoredPassword()
    {
        var account = new Account();

        Assert.That(account.VerifyPassword("anypassword"), Is.False);
    }

    [Test]
    public void VerifyPassword_ReturnsFalse_ForNullOrEmptyInputPassword()
    {
        var account = new Account();
        account.SetPassword("SomePassword123!");

        Assert.That(account.VerifyPassword(null!), Is.False);
        Assert.That(account.VerifyPassword(string.Empty), Is.False);
    }

    [Test]
    public void HashPassword_ProducesDifferentHashes_ForSamePassword()
    {
        // Random salt per call, so same password produces different hashes
        var hash1 = Account.HashPassword("SamePassword123!");
        var hash2 = Account.HashPassword("SamePassword123!");

        Assert.That(hash1, Is.Not.EqualTo(hash2));
    }

    [Test]
    public void HashPassword_ProducesDifferentHashes_ForDifferentSalts()
    {
        var otherSalt = Enumerable.Range(9, 16).Select(i => (byte)i).ToArray();

        Assert.That(Account.HashPassword("P@ssw0rd", FixedSalt), Is.Not.EqualTo(Account.HashPassword("P@ssw0rd", otherSalt)));
    }

    [Test]
    public void HashPassword_Throws_ForWrongSaltLength()
    {
        Assert.Throws<ArgumentException>(() => Account.HashPassword("MySecret123!", Array.Empty<byte>()));
        Assert.Throws<ArgumentException>(() => Account.HashPassword("MySecret123!", new byte[8]));
    }

    [Test]
    public void VerifyPassword_IsCaseSensitive()
    {
        var password = "CaseSensitive123!";
        var account = new Account();
        account.SetPassword(password);

        Assert.That(account.VerifyPassword("casesensitive123!"), Is.False);
        Assert.That(account.VerifyPassword("CASESENSITIVE123!"), Is.False);
        Assert.That(account.VerifyPassword(password), Is.True);
    }

    [Test]
    public void HashPassword_Format_IsPhcStringWithExpectedSizes()
    {
        var hash = Account.HashPassword("Test123!");

        Assert.That(hash, Does.StartWith("$argon2id$v=19$"));
        // $argon2id$v=19$m=...,t=...,p=...$ + base64url salt (16 bytes) + base64url hash (32 bytes)
        Assert.That(hash.Length, Is.EqualTo(97));

        var parts = hash.Split('$');
        Assert.That(Base64UrlDecode(parts[4]).Length, Is.EqualTo(16), "salt size");
        Assert.That(Base64UrlDecode(parts[5]).Length, Is.EqualTo(32), "hash size");
    }

    private static void StoreRawPassword(Account account, string raw)
        => typeof(Account).GetProperty(nameof(Account.Password))!.SetValue(account, raw);

    private static byte[] Base64UrlDecode(string text)
    {
        var base64 = text.Replace('-', '+').Replace('_', '/');
        return (base64.Length % 4) switch
        {
            2 => Convert.FromBase64String($"{base64}=="),
            3 => Convert.FromBase64String($"{base64}" + "="),
            _ => Convert.FromBase64String(base64)
        };
    }
}
