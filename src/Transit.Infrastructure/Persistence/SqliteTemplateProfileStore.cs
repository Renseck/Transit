using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Transit.Core.Abstractions;
using Transit.Core.Entities;
using Transit.Infrastructure.Configuration;

namespace Transit.Infrastructure.Persistence;

public sealed class SqliteTemplateProfileStore : ITemplateProfileStore
{
    private readonly IDbContextFactory<TransitDbContext> _factory;
    private readonly StorageOptions _storage;

    public SqliteTemplateProfileStore(
        IDbContextFactory<TransitDbContext> factory, IOptions<StorageOptions> storage)
    {
        _factory = factory;
        _storage = storage.Value;
    }

    public async Task<IReadOnlyList<TemplateProfile>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.TemplateProfiles
            .AsNoTracking()
            .Include(p => p.ColumnMappings)
            .Include(p => p.CellMappings)
            .OrderByDescending(p => p.CreatedUtc)
            .ToListAsync(ct);
    }

    public async Task<TemplateProfile?> GetActiveAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.TemplateProfiles
            .AsNoTracking()
            .Include(p => p.ColumnMappings)
            .Include(p => p.CellMappings)
            .FirstOrDefaultAsync(p => p.IsActive, ct);
    }

    public async Task<TemplateProfile> CreateAsync(
        TemplateProfile profile, Stream templateBytes, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        profile.CreatedUtc = DateTime.UtcNow;
        profile.TemplateFilePath = string.Empty;   // set once we know the id
        db.TemplateProfiles.Add(profile);
        await db.SaveChangesAsync(ct);

        var dir = Path.Combine(_storage.DataRoot, "templates", profile.Id.ToString());
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "template.xlsx");
        await using (var file = File.Create(path))
        {
            await templateBytes.CopyToAsync(file, ct);
        }

        profile.TemplateFilePath = path;
        await db.SaveChangesAsync(ct);
        return profile;
    }

    public async Task UpdateMappingsAsync(
        int id, IReadOnlyList<ColumnMapping> columns, IReadOnlyList<CellMapping> cells,
        string sheetSelector, int dataStartRow, int dataRowCount, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var profile = await db.TemplateProfiles
            .Include(p => p.ColumnMappings)
            .Include(p => p.CellMappings)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (profile is null)
        {
            return;
        }

        profile.SheetSelector = sheetSelector;
        profile.DataStartRow = dataStartRow;
        profile.DataRowCount = dataRowCount;

        profile.ColumnMappings.Clear();
        foreach (var c in columns)
        {
            profile.ColumnMappings.Add(new ColumnMapping
            {
                TemplateProfileId = id, Field = c.Field, Column = c.Column, Format = c.Format,
            });
        }

        profile.CellMappings.Clear();
        foreach (var c in cells)
        {
            profile.CellMappings.Add(new CellMapping
            {
                TemplateProfileId = id, CellRef = c.CellRef, Source = c.Source, Value = c.Value, Format = c.Format,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var all = await db.TemplateProfiles.ToListAsync(ct);
        foreach (var p in all)
        {
            p.IsActive = p.Id == id;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task RenameAsync(int id, string name, CancellationToken ct = default)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new InvalidOperationException("A template needs a name.");
        }

        await using var db = await _factory.CreateDbContextAsync(ct);
        var profile = await db.TemplateProfiles.FindAsync([id], ct);
        if (profile is null)
        {
            return;
        }

        profile.Name = trimmed;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var profile = await db.TemplateProfiles.FindAsync([id], ct);
        if (profile is null)
        {
            return;
        }

        db.TemplateProfiles.Remove(profile);
        await db.SaveChangesAsync(ct);

        var dir = Path.Combine(_storage.DataRoot, "templates", id.ToString());
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    public async Task<Stream> OpenTemplateAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var profile = await db.TemplateProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new InvalidOperationException("That template profile no longer exists.");

        // Copy to memory so the returned stream outlives the file handle and DbContext.
        var bytes = await File.ReadAllBytesAsync(profile.TemplateFilePath, ct);
        return new MemoryStream(bytes);
    }
}