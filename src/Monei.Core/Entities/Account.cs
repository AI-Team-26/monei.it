using Monei.Core.Security;

namespace Monei.Core.Entities;

public sealed class Account
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    // NULL means "no password set" (login blocked until reset). Otherwise an Argon2id PHC
    // string ($argon2id$v=19$m=...,t=...,p=...$salt$hash), never plain text.
    public string? Password { get; private set; }

    public bool PasswordResetRequired { get; set; }

    public string Role { get; set; } = string.Empty;

    public int CurrencyId { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastChangeDate { get; set; }

    public Guid Guid { get; set; }

    // Setting a password always clears the reset flag, so the two properties can't drift apart.
    public void SetPassword(string plainText)
    {
        Password = PasswordHelper.HashPassword(plainText);
        PasswordResetRequired = false;
    }

    public bool VerifyPassword(string plainText) => PasswordHelper.VerifyPassword(Password, plainText);
}
