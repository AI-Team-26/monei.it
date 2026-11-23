using Monei.Core.Data;
using Monei.Core.Entities;
using Monei.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Monei.Core.Tests;

public class RegistryEntryRepositoryIntegrationTests
{
    private const string ConnectionString =
        "Server=localhost,1433;Database=monei_tests;User Id=sa;Password=monei-Local-Password-1;TrustServerCertificate=True";

    private MoneiDbContext context = null!;

    [OneTimeSetUp]
    public void SetUpDatabase()
    {
        context = new MoneiDbContext(ConnectionString);

        try
        {
            context.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            Assert.Ignore($"SQL Server is not reachable: {ex.Message}");
        }
    }

    [OneTimeTearDown]
    public void TearDownDatabase()
    {
        context?.Dispose();
    }

    private int InsertEntry(DateTime date)
    {
        var entry = new RegistryEntry
        {
            AccountId = 0,
            Amount = 42.50m,
            OperationType = 1,
            CreationDate = DateTime.Now,
            Date = date,
            CategoryId = 0,
            IsTaxDeductible = false,
            IsSpecialEvent = false
        };

        context.RegistryEntries.Add(entry);
        context.SaveChanges();
        return entry.Id;
    }

    private void RemoveEntry(int id)
    {
        var entry = context.RegistryEntries.Find(id);
        if (entry is not null)
        {
            context.RegistryEntries.Remove(entry);
            context.SaveChanges();
        }
    }

    [Test]
    public async Task GetByDateRange_Returns_Entries_In_Range()
    {
        var id = InsertEntry(new DateTime(2025, 6, 15));
        try
        {
            var repo = new RegistryEntryRepository(context);
            var result = await repo.GetByDateRangeAsync(new DateTime(2025, 6, 1), new DateTime(2025, 6, 30));

            Assert.That(result, Has.Some.Matches((RegistryEntry? e) => e is not null && e.Id == id));
        }
        finally
        {
            RemoveEntry(id);
        }
    }

    [Test]
    public async Task GetByDateRange_Excludes_Entries_Outside_Range()
    {
        var id = InsertEntry(new DateTime(2025, 6, 15));
        try
        {
            var repo = new RegistryEntryRepository(context);
            var result = await repo.GetByDateRangeAsync(new DateTime(2025, 7, 1), new DateTime(2025, 7, 31));

            Assert.That(result, Has.No.Matches((RegistryEntry? e) => e is not null && e.Id == id));
        }
        finally
        {
            RemoveEntry(id);
        }
    }

    [Test]
    public async Task GetByDateRange_Includes_Start_Boundary()
    {
        var id = InsertEntry(new DateTime(2025, 6, 1));
        try
        {
            var repo = new RegistryEntryRepository(context);
            var result = await repo.GetByDateRangeAsync(new DateTime(2025, 6, 1), new DateTime(2025, 6, 30));

            Assert.That(result, Has.Some.Matches((RegistryEntry? e) => e is not null && e.Id == id));
        }
        finally
        {
            RemoveEntry(id);
        }
    }

    [Test]
    public async Task GetByDateRange_Includes_End_Boundary()
    {
        var id = InsertEntry(new DateTime(2025, 6, 30));
        try
        {
            var repo = new RegistryEntryRepository(context);
            var result = await repo.GetByDateRangeAsync(new DateTime(2025, 6, 1), new DateTime(2025, 6, 30));

            Assert.That(result, Has.Some.Matches((RegistryEntry? e) => e is not null && e.Id == id));
        }
        finally
        {
            RemoveEntry(id);
        }
    }
}
