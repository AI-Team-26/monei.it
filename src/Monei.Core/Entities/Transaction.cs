namespace Monei.Core.Entities;

public sealed class Transaction
{
    public string Id { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Category { get; set; } = string.Empty;
}