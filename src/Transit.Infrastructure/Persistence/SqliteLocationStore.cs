using Microsoft.EntityFrameworkCore;
using Transit.Core.Abstractions;
using Transit.Core.Entities;

namespace Transit.Infrastructure.Persistence;

public sealed class SqliteLocationStore : ILocationStore
{
    private readonly IDbContextFactory<TransitDbContext> _factory;

    public SqliteLocationStore(IDbContextFactory<TransitDbContext> factory) => _factory = factory;

    public async Task<IReadOnlyList<WorkLocation>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.WorkLocations.AsNoTracking().OrderBy(l => l.Id).ToListAsync(ct);
    }
}