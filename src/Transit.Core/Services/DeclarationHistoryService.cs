using Transit.Core.Abstractions;
using Transit.Core.Declarations;
using Transit.Core.Entities;

namespace Transit.Core.Services;

/// <summary>
/// Owns the run+artifact lifecycle: persist a generate (run row + rows + files), list runs, open an
/// artifact for download, and delete a run (DB rows + its folder). Coordinates the two dumb stores
/// so nothing else juggles DB-and-disk.
/// </summary>
public sealed class DeclarationHistoryService
{
    private readonly IDeclarationRunStore _runs;
    private readonly IArtifactStore _artifacts;

    public DeclarationHistoryService(IDeclarationRunStore runs, IArtifactStore artifacts)
    {
        _runs = runs;
        _artifacts = artifacts;
    }

    // Persist a generate: create the run (+ rows), write the artifacts, record their paths.
    public async Task<DeclarationRun> SaveAsync(
        DeclarationDraft reviewed, TemplateProfile profile, byte[] xlsx, byte[]? pdf, CancellationToken ct = default)
    {
        var run = new DeclarationRun
        {
            PeriodYear = reviewed.Year,
            PeriodMonth = reviewed.Month,
            TemplateProfileId = profile.Id,
            TemplateName = profile.Name,
            CreatedUtc = DateTime.UtcNow,
            XlsxFilePath = string.Empty,   // set once the file is written
            PdfFilePath = null,
            Rows = reviewed.Rows.Select(ToRow).ToList(),
        };

        run = await _runs.CreateAsync(run, ct);

        try
        {
            var xlsxPath = await _artifacts.SaveAsync(run.PeriodYear, run.PeriodMonth, run.Id, "declaration.xlsx", xlsx, ct);
            var pdfPath = pdf is null
                ? null
                : await _artifacts.SaveAsync(run.PeriodYear, run.PeriodMonth, run.Id, "declaration.pdf", pdf, ct);

            await _runs.SetArtifactPathsAsync(run.Id, xlsxPath, pdfPath, ct);
            run.XlsxFilePath = xlsxPath;
            run.PdfFilePath = pdfPath;
            return run;
        }
        catch
        {
            // Compensate: never leave a run row without its artifacts.
            await _runs.DeleteAsync(run.Id, ct);
            throw;
        }
    }

    public Task<IReadOnlyList<DeclarationRunSummary>> GetSummariesAsync(CancellationToken ct = default)
        => _runs.GetSummariesAsync(ct);

    // The bytes of a stored artifact; throws a friendly error when the run or artifact is gone.
    public async Task<byte[]> OpenArtifactAsync(int runId, ArtifactKind kind, CancellationToken ct = default)
    {
        var run = await _runs.GetAsync(runId, ct)
            ?? throw new InvalidOperationException("That declaration run no longer exists.");

        var path = kind == ArtifactKind.Pdf ? run.PdfFilePath : run.XlsxFilePath;
        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException("That file is not available for this run.");
        }

        return await _artifacts.ReadAsync(path, ct);
    }

    // Remove a run: DB rows AND the generated/{…}/{runId}/ folder.
    public async Task DeleteAsync(int runId, CancellationToken ct = default)
    {
        var run = await _runs.GetAsync(runId, ct);
        if (run is null)
        {
            return;
        }

        await _runs.DeleteAsync(run.Id, ct);
        await _artifacts.DeleteRunAsync(run.PeriodYear, run.PeriodMonth, run.Id, ct);
    }

    private static DeclarationRow ToRow(DeclarationDraftRow r) => new()
    {
        Date = r.Date,
        WeekdayName = r.WeekdayName,
        WorkLocationId = r.WorkLocationId,
        LocationName = r.LocationName,
        ColorHex = r.ColorHex,
        CountsAsTravel = r.CountsAsTravel,
        IsUnmarkedDefault = r.IsUnmarkedDefault,
        RouteId = r.SelectedRouteId,
        RouteName = r.RouteName,
        FromLocationName = r.FromLocationName,
        FromPostcode = r.FromPostcode,
        ToLocationName = r.ToLocationName,
        ToPostcode = r.ToPostcode,
        OutboundKm = r.OutboundKm,
        ReturnKm = r.ReturnKm,
        Kilometers = r.Kilometers,
        HasNoRoute = r.HasNoRoute,
    };
}