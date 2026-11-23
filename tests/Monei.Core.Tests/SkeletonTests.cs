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
    [Test]
    public void HashPassword_ReturnsNonEmptyString_ForValidPassword()
    {
        var hash = Account.HashPassword("MySecret123!");

        Assert.That(hash, Is.Not.Empty);
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
        // PBKDF2 uses a random salt, so same password should produce different hashes
        var hash1 = Account.HashPassword("SamePassword123!");
        var hash2 = Account.HashPassword("SamePassword123!");

        Assert.That(hash1, Is.Not.EqualTo(hash2));
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