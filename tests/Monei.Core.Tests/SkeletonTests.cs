using Monei.Core.Configuration;
using Monei.Core.Entities;
using Monei.Core.Repositories;
using Moq;
using NUnit.Framework;

namespace Monei.Core.Tests;

public class SkeletonTests
{
    [Test]
    public void SqlServerSettings_Default_Is_Empty()
    {
        var settings = new SqlServerSettings();

        Assert.That(settings.ConnectionString, Is.Empty);
    }

    [Test]
    public async Task Repository_Can_Be_Mocked()
    {
        var repository = new Mock<IRepository<SampleEntity, string>>();
        repository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SampleEntity>());

        var result = await repository.Object.GetAllAsync();

        Assert.That(result, Is.Empty);
    }

    public sealed class SampleEntity;
}

[TestFixture]
public class AccountPasswordTests
{
    private static readonly byte[] FixedSalt = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();

    // Pre-calculated once for password "MySecret123!" and FixedSalt:
    // Base64(salt + PBKDF2-HMAC-SHA256(password, salt, 10000 iterations, 32 bytes))
    private const string KnownHash = "AAECAwQFBgcICQoLDA0OD4WE/U0ECOBpbnCq8ThvkmYIQzid6Y9+lY+PkH7YwDjp";

    [Test]
    public void HashPassword_MatchesKnownValue_WithFixedSalt()
    {
        var hash = Account.HashPassword("MySecret123!", FixedSalt);

        Assert.That(hash, Is.EqualTo(KnownHash));
    }

    [Test]
    public void HashPassword_Throws_ForNullOrEmptyPassword()
    {
        Assert.Throws<ArgumentException>(() => Account.HashPassword(null!));
        Assert.Throws<ArgumentException>(() => Account.HashPassword(string.Empty));
    }

    [Test]
    public void VerifyPassword_ReturnsTrue_ForCorrectPassword()
    {
        var password = "MySecret123!";
        var account = new Account { Password = Account.HashPassword(password) };

        var result = account.VerifyPassword(password);

        Assert.That(result, Is.True);
    }

    [Test]
    public void VerifyPassword_ReturnsFalse_ForWrongPassword()
    {
        var account = new Account { Password = Account.HashPassword("Correct123!") };

        var result = account.VerifyPassword("Wrong123!");

        Assert.That(result, Is.False);
    }

    [Test]
    public void VerifyPassword_ReturnsFalse_ForEmptyStoredPassword()
    {
        var account = new Account { Password = string.Empty };

        var result = account.VerifyPassword("anypassword");

        Assert.That(result, Is.False);
    }

    [Test]
    public void VerifyPassword_ReturnsFalse_ForNullOrEmptyInputPassword()
    {
        var account = new Account { Password = Account.HashPassword("SomePassword123!") };

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
    public void VerifyPassword_WorksWithCaseSensitiveInput()
    {
        // Base64 is case-sensitive - verify password check works correctly
        var password = "CaseSensitive123!";
        var account = new Account { Password = Account.HashPassword(password) };

        // Wrong case should fail
        Assert.That(account.VerifyPassword("casesensitive123!"), Is.False);
        Assert.That(account.VerifyPassword("CASESENSITIVE123!"), Is.False);
        // Correct case should pass
        Assert.That(account.VerifyPassword(password), Is.True);
    }

    [Test]
    public void HashPassword_Format_IsBase64EncodedSaltAndHash()
    {
        var hash = Account.HashPassword("Test123!");
        var bytes = Convert.FromBase64String(hash);

        // Salt (16) + Hash (32) = 48 bytes minimum
        Assert.That(bytes.Length, Is.GreaterThanOrEqualTo(48));
    }
}