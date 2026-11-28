using Monei.Core.Security;

namespace Monei.Core.Entities;

public sealed class Account
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string? Password { get; private set; }

    public bool PasswordResetRequired { get; set; }

    public string Role { get; set; } = string.Empty;

    public int CurrencyId { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastChangeDate { get; set; }

    public Guid Guid { get; set; }

    public void SetPassword(string plainText)
    {
        Password = PasswordHelper.HashPassword(plainText);
        PasswordResetRequired = false;
    }

    public bool VerifyPassword(string plainText)
        => !PasswordResetRequired && PasswordHelper.VerifyPassword(Password, plainText);
}
