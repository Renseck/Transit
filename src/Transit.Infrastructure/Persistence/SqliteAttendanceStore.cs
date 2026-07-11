using Microsoft.EntityFrameworkCore;
using Transit.Core.Abstractions;
using Transit.Core.Entities;

namespace Transit.Infrastructure.Persistence;

public sealed class SqliteAttendanceStore : IAttendanceStore
{
    private readonly IDbContextFactory<TransitDbContext> _factory;

    public SqliteAttendanceStore(IDbContextFactory<TransitDbContext> factory) => _factory = factory;

    public async Task<IReadOnlyList<AttendanceEntry>> GetRangeAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.AttendanceEntries
            .AsNoTracking()
            .Where(e => e.Date >= from && e.Date <= to)
            .ToListAsync(ct);
    }

    public async Task SetAsync(DateOnly date, int workLocationId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var existing = await db.AttendanceEntries.FindAsync([date], ct);
        if (existing is null)
        {
            db.AttendanceEntries.Add(new AttendanceEntry { Date = date, WorkLocationId = workLocationId });
        }
        else
        {
            existing.WorkLocationId = workLocationId;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task ClearAsync(DateOnly date, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var existing = await db.AttendanceEntries.FindAsync([date], ct);
        if (existing is null)
        {
            return;
        }

        db.AttendanceEntries.Remove(existing);
        await db.SaveChangesAsync(ct);
    }
}