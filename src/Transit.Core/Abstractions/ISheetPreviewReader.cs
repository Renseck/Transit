using Transit.Core.Declarations;

namespace Transit.Core.Abstractions;

public interface ISheetPreviewReader
{
    // Reads a worksheet's used range into display strings, evaluating formula cells where possible.
    // sheetSelector null/blank → the first worksheet (the generated declaration is single-sheet).
    Task<SheetGrid> ReadAsync(Stream xlsx, string? sheetSelector, CancellationToken ct = default);
}