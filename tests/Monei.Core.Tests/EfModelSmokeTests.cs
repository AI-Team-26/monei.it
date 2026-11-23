using Monei.Core.Data;
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
        var entityType = context.Model.FindEntityType(typeof(Monei.Core.Entities.RegistryEntry))!;

        Assert.That(entityType.GetTableName(), Is.EqualTo("Registry"));
        Assert.That(entityType.FindProperty(nameof(Monei.Core.Entities.RegistryEntry.Id))!.GetColumnName() is not null, Is.True);
    }

    [Test]
    public void RegistryEntry_Amount_Is_Decimal()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Monei.Core.Entities.RegistryEntry))!;
        var property = entityType.FindProperty(nameof(Monei.Core.Entities.RegistryEntry.Amount))!;

        Assert.That(property.GetColumnType(), Does.Contain("decimal"));
    }

    [Test]
    public void Account_Maps_To_Account_Table()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Monei.Core.Entities.Account))!;

        Assert.That(entityType.GetTableName(), Is.EqualTo("Account"));
    }

    [Test]
    public void Category_Maps_To_Category_Table()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Monei.Core.Entities.Category))!;

        Assert.That(entityType.GetTableName(), Is.EqualTo("Category"));
    }

    [Test]
    public void Subcategory_Maps_To_Subcategory_Table()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Monei.Core.Entities.Subcategory))!;

        Assert.That(entityType.GetTableName(), Is.EqualTo("Subcategory"));
    }
}
