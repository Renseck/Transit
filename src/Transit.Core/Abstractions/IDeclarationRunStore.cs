using Transit.Core.Declarations;
using Transit.Core.Entities;

namespace Transit.Core.Abstractions;

public interface IDeclarationRunStore
{
    // Newest first; projected — does not materialise the runs' rows.
    Task<IReadOnlyList<DeclarationRunSummary>> GetSummariesAsync(CancellationToken ct = default);

    // The full run including its frozen rows, or null if it no longer exists.
    Task<DeclarationRun?> GetAsync(int id, CancellationToken ct = default);

    // Persists the run and its rows; returns it with the assigned Id.
    Task<DeclarationRun> CreateAsync(DeclarationRun run, CancellationToken ct = default);

    // Records the artifact paths after the files are written.
    Task SetArtifactPathsAsync(int id, string xlsxPath, string? pdfPath, CancellationToken ct = default);

    // Removes the run (cascades to its rows). Files are the artifact store's concern.
    Task DeleteAsync(int id, CancellationToken ct = default);
}