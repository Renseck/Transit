namespace Transit.Core.Declarations;

// The bytes + filenames produced by a generate, plus the id of the persisted run. PdfBytes is null
// when PDF conversion failed but the xlsx succeeded (the page still offers the spreadsheet).
// ExportedPath is where the PDF was copied (null when no export folder is set or the copy failed);
// ExportError says why a configured export failed — the run itself is saved regardless.
public sealed record GeneratedDeclaration(
    byte[] XlsxBytes,
    byte[]? PdfBytes,
    string XlsxFileName,
    string PdfFileName,
    int RunId,
    string? ExportedPath = null,
    string? ExportError = null);