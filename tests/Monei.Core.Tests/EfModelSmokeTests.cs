using Monei.Core.Data;
using Monei.Core.Entities;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Monei.Core.Tests;

public class EfModelSmokeTests
{
    private static MoneiDbContext CreateContext()
        => new("Server=dummy;Database=dummy;User Id=dummy;Password=dummy;TrustServerCertificate=True");

    [Test]
    public void Model_Builds_Without_Errors()
    {
        using var context = CreateContext();
        var model = context.Model;

        Assert.That(model, Is.Not.Null);
        Assert.That(model.GetEntityTypes().Count(), Is.GreaterThanOrEqualTo(4));
    }

    [Test]
    public void RegistryEntry_Maps_To_Registry_Table()
    {
        using var context = CreateContext();
        var found = context.Model.FindEntityType(typeof(RegistryEntry));
        Assert.That(found, Is.Not.Null);
        var entityType = found!;

        Assert.That(entityType.GetTableName(), Is.EqualTo("Registry"));
        Assert.That(entityType.FindPrimaryKey(), Is.Not.Null);
    }

    [Test]
    public void RegistryEntry_Amount_Is_Decimal()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(RegistryEntry))!;
        var property = entityType.FindProperty(nameof(RegistryEntry.Amount))!;

        Assert.That(property.GetColumnType(), Is.EqualTo("decimal(12,5)"));
    }

    [Test]
    public void Account_Maps_To_Account_Table()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Account));

        Assert.That(entityType, Is.Not.Null);
        Assert.That(entityType!.GetTableName(), Is.EqualTo("Account"));
    }

    [Test]
    public void Category_Maps_To_Category_Table()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Category));

        Assert.That(entityType, Is.Not.Null);
        Assert.That(entityType!.GetTableName(), Is.EqualTo("Category"));
    }

    [Test]
    public void Subcategory_Maps_To_Subcategory_Table()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Subcategory));

        Assert.That(entityType, Is.Not.Null);
        Assert.That(entityType!.GetTableName(), Is.EqualTo("Subcategory"));
    }
}
