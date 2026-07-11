using Microsoft.EntityFrameworkCore;
using Transit.Core.Abstractions;
using Transit.Core.Entities;

namespace Transit.Infrastructure.Persistence;

public sealed class SqliteLocationStore : ILocationStore
{
    private readonly IDbContextFactory<TransitDbContext> _factory;

    public SqliteLocationStore(IDbContextFactory<TransitDbContext> factory) => _factory = factory;

    public async Task<IReadOnlyList<WorkLocation>> GetAllAsync(
        bool includeArchived = false, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var query = db.WorkLocations.AsNoTracking().AsQueryable();
        if (!includeArchived)
        {
            query = query.Where(l => !l.IsArchived);
        }

        return await query.OrderBy(l => l.DisplayOrder).ThenBy(l => l.Id).ToListAsync(ct);
    }

    public async Task<WorkLocation> AddAsync(WorkLocation location, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.WorkLocations.Add(location);
        await db.SaveChangesAsync(ct);
        return location;
    }

    public async Task UpdateAsync(WorkLocation location, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.WorkLocations.Update(location);
        await db.SaveChangesAsync(ct);
    }

    public async Task SetDefaultAsync(int locationId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var all = await db.WorkLocations.ToListAsync(ct);
        foreach (var l in all)
        {
            l.IsDefaultForUnmarked = l.Id == locationId;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task SetArchivedAsync(int locationId, bool archived, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var loc = await db.WorkLocations.FindAsync([locationId], ct);
        if (loc is null)
        {
            return;
        }

        loc.IsArchived = archived;
        await db.SaveChangesAsync(ct);
    }

    public async Task ReorderAsync(IReadOnlyList<int> orderedIds, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var all = await db.WorkLocations.ToListAsync(ct);
        for (var i = 0; i < orderedIds.Count; i++)
        {
            var loc = all.FirstOrDefault(l => l.Id == orderedIds[i]);
            if (loc is not null)
            {
                loc.DisplayOrder = i;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}