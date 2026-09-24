using System.Globalization;
using Transit.Core.Declarations;
using Transit.Core.Entities;

namespace Transit.Core.Templates;

/// <summary>
/// Pure mapping resolution. Turns a declaration row + field into the raw typed value to write
/// (never a pre-stringified number — the writer applies Format as a cell number-format so
/// Kilometers stays numeric and the template SUM keeps working), a header cell into its value,
/// and computes how many rows the writer must insert for a month that overflows the template.
/// No I/O, no ClosedXML.
/// </summary>
public static class TemplateMapping
{
    private static readonly CultureInfo Dutch = CultureInfo.GetCultureInfo("nl-NL");

    // The raw value for a repeating-row field, or null if the row can't supply it.
    public static object? ResolveColumnValue(DeclarationDraftRow row, TemplateColumnField field) => field switch
    {
        TemplateColumnField.Date => row.Date,
        TemplateColumnField.WeekdayName => row.WeekdayName,
        TemplateColumnField.FromLocation => row.FromLocationName,
        TemplateColumnField.ToLocation => row.ToLocationName,
        TemplateColumnField.FromPostcode => row.FromPostcode,
        TemplateColumnField.ToPostcode => row.ToPostcode,
        TemplateColumnField.RouteName => row.RouteName,
        TemplateColumnField.OutboundKm => row.OutboundKm,
        TemplateColumnField.ReturnKm => row.ReturnKm,
        TemplateColumnField.Kilometers => row.Kilometers,
        _ => null,
    };

    private const string DefaultSignatureDateFormat = "dd/MM/yyyy";

    // The value for a static header cell, or null if a static source has no literal. Signature
    // sources resolve only when signedOn is given (the signed PDF's workbook); unsigned they stay
    // null so the template's cells are left blank. The signature date is a pre-formatted string, not
    // a date + number-format: a "/" in a sheet number-format follows the locale separator.
    public static object? ResolveCellValue(
        CellMapping cell, int year, int month, DateOnly? signedOn = null, string? employeeName = null) => cell.Source switch
    {
        TemplateCellSource.MonthName => new DateTime(year, month, 1).ToString("MMMM", Dutch),
        TemplateCellSource.Year => year,
        TemplateCellSource.SignatureName => signedOn is null ? null : Literal(cell) ?? Blank(employeeName),
        TemplateCellSource.SignatureDate => signedOn?.ToString(
            Blank(cell.Format) ?? DefaultSignatureDateFormat, CultureInfo.InvariantCulture),
        _ => Literal(cell),
    };

    // The configured employee name (first non-blank EmployeeName literal), or null when unmapped.
    public static string? EmployeeName(IEnumerable<CellMapping> cells) => cells
        .Where(c => c.Source == TemplateCellSource.EmployeeName)
        .Select(Literal)
        .FirstOrDefault(name => name is not null);

    // Whether the profile signs its PDF: a SignatureName cell is mapped.
    public static bool HasSignature(IEnumerable<CellMapping> cells)
        => cells.Any(c => c.Source == TemplateCellSource.SignatureName);

    private static string? Literal(CellMapping cell) => Blank(cell.Value);

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // How many rows the writer must insert so a heavy month fits (0 when within capacity).
    public static int RowsToInsert(int travelRowCount, int dataRowCount)
        => Math.Max(0, travelRowCount - dataRowCount);
}