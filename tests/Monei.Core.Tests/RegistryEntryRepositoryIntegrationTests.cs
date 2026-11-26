using Monei.Core.Data;
using Monei.Core.Entities;
using Monei.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Monei.Core.Tests;

// Runs against the real local SQL Server from docker-compose.yml (database "monei"),
// so the EF model is checked against the actual schema.
// Skips gracefully when the server or the database is not available.
public class RegistryEntryRepositoryIntegrationTests
{
    private const string DefaultConnectionString =
        "Server=localhost,1433;Database=monei;User Id=sa;Password=monei-Local-Password-1;TrustServerCertificate=True";

    private const string NoteMarker = "__monei_test__";

    private MoneiDbContext context = null!;

    [OneTimeSetUp]
    public void SetUpDatabase()
    {
        var connectionString = Environment.GetEnvironmentVariable("MONEI_TEST_SQL_CONNECTION_STRING") ?? DefaultConnectionString;
        context = new MoneiDbContext(connectionString);

        try
        {
            context.Database.GetDbConnection();
            var registryExists = context.Database
                .SqlQuery<int>($"SELECT CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Registry') THEN 1 ELSE 0 END AS Value")
                .Single();
            if (registryExists == 0)
            {
                Assert.Ignore("The 'monei' database has no 'Registry' table");
            }
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

    private static RegistryEntry NewEntry(DateTime date) => new()
    {
        AccountId = 0,
        Amount = 42.50m,
        OperationType = 1,
        CreationDate = DateTime.Now,
        Date = date,
        CategoryId = 0,
        Note = NoteMarker,
        IsTaxDeductible = false,
        IsSpecialEvent = false
    };

    private sealed record ColumnInfo(string ColumnName, string ColumnType);

    [Test]
    public void Model_Matches_Registry_Table_Schema()
    {
        var modelColumns = context.Model
            .FindEntityType(typeof(RegistryEntry))!
            .GetProperties()
            .ToDictionary(p => p.GetColumnName()!, p => p.GetColumnType()!);

        var dbColumns = context.Database
            .SqlQuery<ColumnInfo>(
                $"""
                SELECT
                    c.COLUMN_NAME AS ColumnName,
                    CASE
                        WHEN c.DATA_TYPE IN ('decimal', 'numeric')
                            THEN c.DATA_TYPE + '(' + CAST(c.NUMERIC_PRECISION AS varchar) + ',' + CAST(c.NUMERIC_SCALE AS varchar) + ')'
                        WHEN c.DATA_TYPE IN ('nvarchar', 'varchar', 'nchar', 'char')
                            THEN c.DATA_TYPE + '(' + CAST(c.CHARACTER_MAXIMUM_LENGTH AS varchar) + ')'
                        ELSE c.DATA_TYPE
                    END AS ColumnType
                FROM INFORMATION_SCHEMA.COLUMNS c
                WHERE c.TABLE_NAME = 'Registry'
                """)
            .ToList()
            .ToDictionary(c => c.ColumnName, c => c.ColumnType);

        Assert.That(dbColumns.Keys, Is.SupersetOf(modelColumns.Keys), "Registry table is missing columns the model maps to");

        foreach (var (name, type) in modelColumns)
        {
            Assert.That(dbColumns[name], Is.EqualTo(type), $"Column '{name}' type does not match the model");
        }
    }

    [Test]
    public async Task GetByDateRange_Returns_Entries_In_Range_Including_Boundaries()
    {
        var repo = new RegistryEntryRepository(context);
        var start = new DateTime(2025, 6, 1);
        var middle = new DateTime(2025, 6, 15);
        var end = new DateTime(2025, 6, 30);
        var ids = new List<int>();

        try
        {
            foreach (var date in new[] { start, middle, end })
            {
                ids.Add((await repo.CreateAsync(NewEntry(date))).Id);
            }

            var result = await repo.GetByDateRangeAsync(start, end);
            var foundDates = result.Where(e => ids.Contains(e.Id)).Select(e => e.Date).ToHashSet();

            Assert.That(foundDates, Is.EquivalentTo(new[] { start, middle, end }));
        }
        finally
        {
            foreach (var id in ids)
            {
                await repo.DeleteAsync(id);
            }
        }
    }

    [Test]
    public async Task GetByDateRange_Excludes_Entries_Outside_Range()
    {
        var repo = new RegistryEntryRepository(context);
        var id = (await repo.CreateAsync(NewEntry(new DateTime(2025, 6, 15)))).Id;

        try
        {
            var result = await repo.GetByDateRangeAsync(new DateTime(2025, 7, 1), new DateTime(2025, 7, 31));

            Assert.That(result.Any(e => e.Id == id), Is.False);
        }
        finally
        {
            await repo.DeleteAsync(id);
        }
    }
}
