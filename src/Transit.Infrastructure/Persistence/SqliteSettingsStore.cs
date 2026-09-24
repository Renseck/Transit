using Microsoft.EntityFrameworkCore;
using Transit.Core.Abstractions;
using Transit.Core.Entities;

namespace Transit.Infrastructure.Persistence;

// The single AppSettings row. No seed: a missing row simply means "defaults".
public sealed class SqliteSettingsStore : ISettingsStore
{
    private readonly IDbContextFactory<TransitDbContext> _factory;

    public SqliteSettingsStore(IDbContextFactory<TransitDbContext> factory) => _factory = factory;

    public async Task<AppSettings> GetAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == AppSettings.SingletonId, ct)
            ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        settings.Id = AppSettings.SingletonId;

        var exists = await db.AppSettings.AnyAsync(s => s.Id == AppSettings.SingletonId, ct);
        if (exists)
        {
            db.AppSettings.Update(settings);
        }
        else
        {
            db.AppSettings.Add(settings);
        }

        await db.SaveChangesAsync(ct);
    }
}
