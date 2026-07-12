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

    // The value for a static header cell, or null if a static source has no literal.
    public static object? ResolveCellValue(CellMapping cell, int year, int month) => cell.Source switch
    {
        TemplateCellSource.MonthName => new DateTime(year, month, 1).ToString("MMMM", Dutch),
        TemplateCellSource.Year => year,
        _ => string.IsNullOrWhiteSpace(cell.Value) ? null : cell.Value.Trim(),
    };

    // How many rows the writer must insert so a heavy month fits (0 when within capacity).
    public static int RowsToInsert(int travelRowCount, int dataRowCount)
        => Math.Max(0, travelRowCount - dataRowCount);
}