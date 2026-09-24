using ClosedXML.Excel;
using Microsoft.Extensions.Options;
using Transit.Core.Abstractions;
using Transit.Core.Declarations;
using Transit.Core.Entities;
using Transit.Core.Templates;
using Transit.Infrastructure.Configuration;

namespace Transit.Infrastructure.Excel;

/// <summary>
/// Fills the uploaded employer template in place: writes header cells and one row per travel day,
/// auto-inserting rows when the month exceeds the template's physical data rows so the template's
/// own SUM expands. Never rebuilds the workbook; never writes a monthly total. With a signing date it
/// also writes the signature cells, the name in the configured handwriting font.
/// </summary>
public sealed class ClosedXmlTemplateWriter : IExcelWriter
{
    private readonly SignatureOptions _signature;

    public ClosedXmlTemplateWriter(IOptions<SignatureOptions> signature)
    {
        _signature = signature.Value;
    }

    public Task<byte[]> FillAsync(
        TemplateProfile profile, Stream templateBytes, DeclarationDraft draft,
        DateOnly? signedOn = null, CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook(templateBytes);
        var sheet = SelectSheet(workbook, profile.SheetSelector);

        WriteHeaderCells(sheet, profile, draft, signedOn);

        var travelRows = draft.Rows.Where(r => r.CountsAsTravel).OrderBy(r => r.Date).ToList();
        var toInsert = TemplateMapping.RowsToInsert(travelRows.Count, profile.DataRowCount);
        if (toInsert > 0)
        {
            // Insert above the last physical data row so SUM(...:lastRow) expands to cover them.
            var lastDataRow = profile.DataStartRow + profile.DataRowCount - 1;
            sheet.Row(lastDataRow).InsertRowsAbove(toInsert);
        }

        WriteDataRows(sheet, profile, travelRows);

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return Task.FromResult(output.ToArray());
    }

    private static IXLWorksheet SelectSheet(XLWorkbook workbook, string selector)
    {
        if (int.TryParse(selector, out var index) && index >= 1 && index <= workbook.Worksheets.Count)
        {
            return workbook.Worksheet(index);
        }

        return workbook.Worksheet(selector);
    }

    private void WriteHeaderCells(IXLWorksheet sheet, TemplateProfile profile, DeclarationDraft draft, DateOnly? signedOn)
    {
        var employeeName = TemplateMapping.EmployeeName(profile.CellMappings);
        foreach (var mapping in profile.CellMappings)
        {
            var value = TemplateMapping.ResolveCellValue(mapping, draft.Year, draft.Month, signedOn, employeeName);
            if (value is null)
            {
                continue;
            }

            var cell = sheet.Cell(mapping.CellRef);
            if (mapping.Source == TemplateCellSource.SignatureDate)
            {
                // Already formatted by the resolver; its Format is a .NET date format, not a sheet format.
                cell.Value = value.ToString();
                continue;
            }

            SetCell(cell, value, mapping.Format);
            if (mapping.Source == TemplateCellSource.SignatureName)
            {
                cell.Style.Font.FontName = _signature.FontName;
                cell.Style.Font.FontSize = _signature.FontSize;
            }
        }
    }

    private static void WriteDataRows(
        IXLWorksheet sheet, TemplateProfile profile, IReadOnlyList<DeclarationDraftRow> travelRows)
    {
        for (var i = 0; i < travelRows.Count; i++)
        {
            var rowNumber = profile.DataStartRow + i;
            foreach (var mapping in profile.ColumnMappings)
            {
                var value = TemplateMapping.ResolveColumnValue(travelRows[i], mapping.Field);
                if (value is null)
                {
                    continue;
                }

                SetCell(sheet.Cell($"{mapping.Column}{rowNumber}"), value, mapping.Format);
            }
        }
    }

    private static void SetCell(IXLCell cell, object value, string? format)
    {
        switch (value)
        {
            case DateOnly date:
                cell.Value = date.ToDateTime(TimeOnly.MinValue);
                break;
            case decimal number:
                cell.Value = number;
                break;
            case int integer:
                cell.Value = integer;
                break;
            default:
                cell.Value = value.ToString();
                break;
        }

        if (!string.IsNullOrWhiteSpace(format))
        {
            cell.Style.NumberFormat.Format = format;
        }
    }
}