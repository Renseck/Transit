using Microsoft.EntityFrameworkCore;
using Transit.Core.Abstractions;
using Transit.Core.Declarations;
using Transit.Core.Entities;

namespace Transit.Infrastructure.Persistence;

public sealed class SqliteDeclarationRunStore : IDeclarationRunStore
{
    private readonly IDbContextFactory<TransitDbContext> _factory;

    public SqliteDeclarationRunStore(IDbContextFactory<TransitDbContext> factory) => _factory = factory;

    public async Task<IReadOnlyList<DeclarationRunSummary>> GetSummariesAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.DeclarationRuns
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedUtc)
            .Select(r => new DeclarationRunSummary(
                r.Id, r.PeriodYear, r.PeriodMonth, r.CreatedUtc, r.TemplateName,
                r.Rows.Count(x => x.CountsAsTravel),
                r.XlsxFilePath != "",
                r.PdfFilePath != null,
                r.Rows.Where(x => x.CountsAsTravel).Sum(x => x.Kilometers)))
            .ToListAsync(ct);
    }

    public async Task<DeclarationRun?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.DeclarationRuns
            .AsNoTracking()
            .Include(r => r.Rows)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<DeclarationRun> CreateAsync(DeclarationRun run, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.DeclarationRuns.Add(run);
        await db.SaveChangesAsync(ct);
        return run;
    }

    public async Task SetArtifactPathsAsync(int id, string xlsxPath, string? pdfPath, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var run = await db.DeclarationRuns.FindAsync([id], ct);
        if (run is null)
        {
            return;
        }

        run.XlsxFilePath = xlsxPath;
        run.PdfFilePath = pdfPath;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var run = await db.DeclarationRuns.FindAsync([id], ct);
        if (run is null)
        {
            return;
        }

        db.DeclarationRuns.Remove(run);
        await db.SaveChangesAsync(ct);
    }
}