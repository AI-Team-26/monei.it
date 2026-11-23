using Monei.Core.Data;
using Monei.Core.Entities;
using Monei.Core.Repositories;
using NUnit.Framework;

namespace Monei.Core.Tests;

[TestFixture]
public class RegistryEntryRepositoryIntegrationTests
{
    private const string ConnectionString = "Server=localhost,1433;Database=monei_it;User Id=sa;Password=monei-Local-Password-1;TrustServerCertificate=True";

    private MoneiDbContext _context = null!;
    private RegistryEntryRepository _repo = null!;

    [SetUp]
    public async Task SetUp()
    {
        // Skip all tests if SQL Server is not reachable.
        try
        {
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
            await conn.OpenAsync();
        }
        catch (Exception ex)
        {
            Assert.Ignore($"SQL Server not reachable at localhost:1433 ({ex.Message})");
        }

        _context = new MoneiDbContext(ConnectionString);
        await _context.Database.EnsureCreatedAsync();
        _repo = new RegistryEntryRepository(_context);

        // Clean up any leftover data from previous runs.
        _context.RegistryEntries.RemoveRange(_context.RegistryEntries);
        await _context.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_context is not null)
        {
            _context.RegistryEntries.RemoveRange(_context.RegistryEntries);
            await _context.SaveChangesAsync();
            await _context.DisposeAsync();
        }
    }

    [Test]
    public async Task GetByDateRange_Returns_Only_Entries_In_Range()
    {
        var jan1 = new DateTime(2025, 1, 1);
        var mar15 = new DateTime(2025, 3, 15);
        var jun30 = new DateTime(2025, 6, 30);

        Seed(jan1, 10m, 1);
        Seed(mar15, 20m, 0);
        Seed(jun30, 30m, -1);
        // Outside the range on both sides.
        Seed(new DateTime(2024, 12, 31), 99m, 1);
        Seed(new DateTime(2025, 7, 1), 88m, 1);

        var result = await _repo.GetByDateRangeAsync(jan1, jun30);

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.Select(e => e.Amount), Is.EquivalentTo(new[] { 10m, 20m, 30m }));
    }

    [Test]
    public async Task GetByDateRange_Includes_Boundary_Dates()
    {
        var start = new DateTime(2025, 5, 1);
        var end = new DateTime(2025, 5, 31);

        Seed(start, 5m, 1);
        Seed(end, 15m, -1);

        var result = await _repo.GetByDateRangeAsync(start, end);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetByDateRange_Empty_When_No_Matches()
    {
        Seed(new DateTime(2025, 1, 1), 10m, 1);

        var result = await _repo.GetByDateRangeAsync(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.That(result, Is.Empty);
    }

    private void Seed(DateTime date, decimal amount, short operationType)
    {
        _context.RegistryEntries.Add(new RegistryEntry
        {
            AccountId = 1,
            Amount = amount,
            OperationType = operationType,
            CreationDate = DateTime.UtcNow,
            Date = date,
            CategoryId = 1,
        });
        _context.SaveChanges();
    }
}
