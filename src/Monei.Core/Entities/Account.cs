namespace Monei.Core.Entities;

public sealed class Account
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int CurrencyId { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastChangeDate { get; set; }

    public System.Guid Guid { get; set; }
}
