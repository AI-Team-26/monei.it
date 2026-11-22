namespace Monei.Core.Entities;

public sealed class RegistryEntry
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public decimal Amount { get; set; }

    // -1 Outcome, 0 Transfer, 1 Income
    public short OperationType { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastChangeDate { get; set; }

    public DateTime Date { get; set; }

    public int CategoryId { get; set; }

    public int? SubCategoryId { get; set; }

    public string? Note { get; set; }

    public bool IsTaxDeductible { get; set; }

    public bool IsSpecialEvent { get; set; }
}
