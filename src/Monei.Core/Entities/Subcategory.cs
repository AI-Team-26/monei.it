namespace Monei.Core.Entities;

public sealed class Subcategory
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreationDate { get; set; }

    public DateTime? LastChangeDate { get; set; }

    public DateTime? DeletionDate { get; set; }
}
