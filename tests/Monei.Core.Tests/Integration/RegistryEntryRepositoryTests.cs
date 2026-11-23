using Monei.Core.Data;
using Monei.Core.Entities;
using Monei.Core.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Monei.Core.Tests.Integration;

// Integration tests that run against the local SQL Server started with docker-compose.
// Each fixture creates a throwaway database built from the EF model, so it also proves
// the model generates valid DDL. The database is dropped again in teardown.
public class RegistryEntryRepositoryTests
{
    private const string DefaultConnectionString = "Server=localhost,1433;User Id=sa;Password=monei-Local-Password-1;TrustServerCertificate=True;Connect Timeout=5";

    private readonly string connectionString;
    private readonly string databaseName = $"monei_it_{Guid.NewGuid():N}";
    private bool schemaCreated;
    private MoneiDbContext? context;

    public RegistryEntryRepositoryTests()
    {
        this.connectionString = Environment.GetEnvironmentVariable("MONEI_SQL_CONNECTION_STRING") ?? DefaultConnectionString;
    }

    [OneTimeSetUp]
    public async Task SetUp()
    {
        Assume.That(await CanConnectAsync(), "local SQL Server is not reachable");

        await ExecuteOnMasterAsync($"CREATE DATABASE [{databaseName}]");

        var options = new DbContextOptionsBuilder<MoneiDbContext>()
            .UseSqlServer($"{this.connectionString.TrimEnd(';')};Initial Catalog={this.databaseName}")
            .Options;
        var context = new MoneiDbContext(options);
        Assert.That(await context.Database.EnsureCreatedAsync(), Is.True, "EF model did not create the schema");

        this.context = context;
        this.schemaCreated = true;
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        if (!this.schemaCreated)
        {
            return;
        }

        this.context?.Dispose();
        SqlConnection.ClearAllPools();

        try
        {
            await ExecuteOnMasterAsync($"DROP DATABASE IF EXISTS [{databaseName}]");
        }
        catch (SqlException)
        {
            // Best effort cleanup; a leftover throwaway database does not break later runs.
        }
    }

    [Test]
    public async Task GetByDateRange_Returns_Only_Entries_Between_Start_And_End_Inclusive()
    {
        // Assigned by OneTimeSetUp before any test runs.
        var repository = new RegistryEntryRepository(this.context!);

        var start = new DateTime(2026, 12, 1);
        var end = new DateTime(2027, 1, 31);

        var expectedIds = new List<int>();
        foreach (var date in new[] { new DateTime(2026, 11, 30), start, new DateTime(2026, 12, 15), end, new DateTime(2027, 2, 1) })
        {
            var entry = await repository.CreateAsync(new RegistryEntry
            {
                AccountId = 1,
                CategoryId = 1,
                Amount = 10m,
                OperationType = 1,
                CreationDate = DateTime.Now,
                Date = date,
            });
            if (date >= start && date <= end)
            {
                expectedIds.Add(entry.Id);
            }
        }

        var result = await repository.GetByDateRangeAsync(start, end);

        Assert.That(result.Select(x => x.Id).OrderBy(x => x), Is.EqualTo(expectedIds.OrderBy(x => x)));
    }

    private async Task<bool> CanConnectAsync()
    {
        try
        {
            using var connection = new SqlConnection(this.connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch (SqlException)
        {
            return false;
        }
    }

    private async Task ExecuteOnMasterAsync(string sql)
    {
        using var connection = new SqlConnection(this.connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
