using Microsoft.EntityFrameworkCore;
using Transit.Core.Abstractions;
using Transit.Core.Entities;

namespace Transit.Infrastructure.Persistence;

public sealed class SqliteRouteStore : IRouteStore
{
    private readonly IDbContextFactory<TransitDbContext> _factory;

    public SqliteRouteStore(IDbContextFactory<TransitDbContext> factory) => _factory = factory;

    public async Task<IReadOnlyList<Route>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Routes
            .AsNoTracking()
            .OrderBy(r => r.ToLocationId)
            .ThenBy(r => r.Name)
            .ToListAsync(ct);
    }

    public async Task<Route> AddAsync(Route route, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Routes.Add(route);
        await db.SaveChangesAsync(ct);
        return route;
    }

    public async Task UpdateAsync(Route route, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Routes.Update(route);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int routeId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var route = await db.Routes.FindAsync([routeId], ct);
        if (route is null)
        {
            return;
        }

        db.Routes.Remove(route);
        await db.SaveChangesAsync(ct);
    }

    public async Task SetDefaultAsync(int routeId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var target = await db.Routes.FindAsync([routeId], ct);
        if (target is null)
        {
            return;
        }

        // Scope the swap to this destination only — default is per-ToLocationId.
        var group = await db.Routes.Where(r => r.ToLocationId == target.ToLocationId).ToListAsync(ct);
        foreach (var r in group)
        {
            r.IsDefault = r.Id == routeId;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}