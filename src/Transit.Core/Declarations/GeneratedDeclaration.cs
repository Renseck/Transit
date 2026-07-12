namespace Transit.Core.Declarations;

// The bytes + filenames produced by a generate. PdfBytes is null when PDF conversion failed
// but the xlsx succeeded (the page still offers the spreadsheet).
public sealed record GeneratedDeclaration(
    byte[] XlsxBytes,
    byte[]? PdfBytes,
    string XlsxFileName,
    string PdfFileName);