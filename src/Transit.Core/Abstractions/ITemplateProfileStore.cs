using Transit.Core.Entities;

namespace Transit.Core.Abstractions;

public interface ITemplateProfileStore
{
    Task<IReadOnlyList<TemplateProfile>> GetAllAsync(CancellationToken ct = default);
    Task<TemplateProfile?> GetActiveAsync(CancellationToken ct = default);

    // Persists the profile row and writes the uploaded .xlsx to disk; returns the saved profile.
    Task<TemplateProfile> CreateAsync(TemplateProfile profile, Stream templateBytes, CancellationToken ct = default);

    // Replaces the profile's mappings and its sheet/start-row/row-count scalars.
    Task UpdateMappingsAsync(
        int id, IReadOnlyList<ColumnMapping> columns, IReadOnlyList<CellMapping> cells,
        string sheetSelector, int dataStartRow, int dataRowCount, CancellationToken ct = default);

    // Atomic single-active swap: demote all, promote this one.
    Task SetActiveAsync(int id, CancellationToken ct = default);

    // Removes the row and the stored template folder.
    Task DeleteAsync(int id, CancellationToken ct = default);

    // Opens the stored template .xlsx for reading.
    Task<Stream> OpenTemplateAsync(int id, CancellationToken ct = default);
}