using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Monei.Core.Data;
using Monei.Core.Entities;
using Monei.Core.Repositories;
using NUnit.Framework;

namespace Monei.Core.Tests;

// Integration test for GetByDateRangeAsync against the local SQL Server (see docker-compose.yml).
// The test database is created on first run and is separate from the development database.
public class GetByDateRangeAsyncTests
{
    private const string ConnectionString =
        "Server=localhost,1433;User Id=sa;Password=monei-Local-Password-1;TrustServerCertificate=True;Database=monei_itest";

    [OneTimeSetUp]
    public async Task EnsureTestDatabaseExists()
    {
        var masterConnectionString = ConnectionString.Replace("Database=monei_itest", "Database=master");
        try
        {
            await using var connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand("IF DB_ID(N'monei_itest') IS NULL CREATE DATABASE [monei_itest]", connection);
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException)
        {
            Assert.Ignore("SQL Server is not reachable at localhost:1433");
        }
    }

    [Test]
    public async Task GetByDateRangeAsync_Returns_Only_Entries_Within_The_Boundaries()
    {
        var context = new MoneiDbContext(ConnectionString);
        await context.Database.EnsureCreatedAsync();
        await context.RegistryEntries.ExecuteDeleteAsync();

        var start = new DateTime(2026, 1, 15);
        var end = new DateTime(2026, 1, 20);

        context.RegistryEntries.AddRange(
            CreateEntry(new DateTime(2026, 1, 10), "before"),
            CreateEntry(start, "start boundary"),
            CreateEntry(new DateTime(2026, 1, 18), "inside"),
            CreateEntry(end, "end boundary"),
            CreateEntry(new DateTime(2026, 1, 25), "after"));
        await context.SaveChangesAsync();

        var repository = new RegistryEntryRepository(context);
        var result = await repository.GetByDateRangeAsync(start, end);

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.Select(e => e.Note), Is.EquivalentTo(new[] { "start boundary", "inside", "end boundary" }));
    }

    private static RegistryEntry CreateEntry(DateTime date, string note) => new()
    {
        AccountId = 1,
        Amount = 10m,
        OperationType = 1,
        CreationDate = DateTime.UtcNow,
        Date = date,
        CategoryId = 1,
        Note = note,
    };
}
