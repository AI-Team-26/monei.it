using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Monei.Core.Data;
using Monei.Core.Entities;
using Monei.Core.Repositories;
using NUnit.Framework;

namespace Monei.Core.Tests;

// Integration tests for RegistryEntryRepository against the local SQL Server
// from docker-compose. They create an isolated test database, so they never
// touch the real `monei` database. The whole fixture is skipped when the
// server is not reachable.
public class RegistryEntryRepositoryIntegrationTests
{
    private const string TestDatabase = "monei_int_test";

    private static readonly string Server = "127.0.0.1,1433";

    private MoneiDbContext context = null!;
    private RegistryEntryRepository repository = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        if (!IsServerReachable())
        {
            Assert.Ignore("SQL Server is not reachable; skipping integration tests.");
        }

        EnsureTestDatabase();

        var options = new DbContextOptionsBuilder<MoneiDbContext>()
            .UseSqlServer(BuildConnectionString(TestDatabase))
            .Options;
        context = new MoneiDbContext(options);
        context.Database.EnsureCreated();
        repository = new RegistryEntryRepository(context);

        context.RegistryEntries.ExecuteDeleteAsync().GetAwaiter().GetResult();

        // Id is an identity column, so it is left for the database to assign.
        context.RegistryEntries.AddRange(
            CreateEntry(new DateTime(2025, 1, 10), 100m),
            CreateEntry(new DateTime(2025, 2, 15), 200m),
            CreateEntry(new DateTime(2025, 3, 20), 300m));
        context.SaveChanges();
    }

    [Test]
    public async Task GetByDateRange_Returns_Only_Entries_Inside_Range()
    {
        var result = await repository.GetByDateRangeAsync(new DateTime(2025, 1, 1), new DateTime(2025, 2, 28));

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(x => x.Date).Order(),
            Is.EquivalentTo(new[] { new DateTime(2025, 1, 10), new DateTime(2025, 2, 15) }));
    }

    [Test]
    public async Task GetByDateRange_Excludes_Entries_Outside_Range()
    {
        var result = await repository.GetByDateRangeAsync(new DateTime(2025, 2, 16), new DateTime(2025, 3, 31));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Date, Is.EqualTo(new DateTime(2025, 3, 20)));
    }

    [Test]
    public async Task GetByDateRange_Returns_Empty_When_No_Entries_Match()
    {
        var result = await repository.GetByDateRangeAsync(new DateTime(2020, 1, 1), new DateTime(2020, 12, 31));

        Assert.That(result, Is.Empty);
    }

    private static RegistryEntry CreateEntry(DateTime date, decimal amount)
        => new()
        {
            AccountId = 1,
            Amount = amount,
            OperationType = 1,
            CreationDate = DateTime.Now,
            Date = date,
            CategoryId = 1,
        };

    private static string BuildConnectionString(string database)
        => $"Server={Server};User Id=sa;Password=monei-Local-Password-1;Database={database};TrustServerCertificate=True;Encrypt=True";

    private static string MasterConnectionString
        => $"Server={Server};User Id=sa;Password=monei-Local-Password-1;Database=master;TrustServerCertificate=True;Encrypt=True";

    private static void EnsureTestDatabase()
    {
        using var connection = new SqlConnection(MasterConnectionString);
        connection.Open();
        using var command = new SqlCommand(
            $"IF DB_ID(N'{TestDatabase}') IS NULL CREATE DATABASE [{TestDatabase}];", connection);
        command.ExecuteNonQuery();
    }

    private static bool IsServerReachable()
    {
        try
        {
            using var connection = new SqlConnection(MasterConnectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
