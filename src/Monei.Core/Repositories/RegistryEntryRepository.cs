using Monei.Core.Data;
using Monei.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Monei.Core.Repositories;

public sealed class RegistryEntryRepository : IRepository<RegistryEntry, int>
{
    private readonly MoneiDbContext context;

    public RegistryEntryRepository(MoneiDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<RegistryEntry>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.RegistryEntries.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<RegistryEntry?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await context.RegistryEntries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<RegistryEntry> CreateAsync(RegistryEntry entity, CancellationToken cancellationToken = default)
    {
        context.RegistryEntries.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(RegistryEntry entity, CancellationToken cancellationToken = default)
    {
        context.RegistryEntries.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await context.RegistryEntries.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return;
        }

        context.RegistryEntries.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RegistryEntry>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
        => await context.RegistryEntries.AsNoTracking().Where(x => x.Date >= start && x.Date <= end).ToListAsync(cancellationToken);
}
