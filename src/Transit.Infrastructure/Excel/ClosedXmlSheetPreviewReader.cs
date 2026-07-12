using ClosedXML.Excel;
using Transit.Core.Abstractions;
using Transit.Core.Declarations;

namespace Transit.Infrastructure.Excel;

/// <summary>
/// Reads a worksheet's used range into a <see cref="SheetGrid"/> of display strings. Formula cells
/// are evaluated (ClosedXML's engine handles SUM + arithmetic); an un-evaluable cell falls back to
/// its formula text. Read-only — never writes.
/// </summary>
public sealed class ClosedXmlSheetPreviewReader : ISheetPreviewReader
{
    public Task<SheetGrid> ReadAsync(Stream xlsx, string? sheetSelector, CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook(xlsx);
        var sheet = SelectSheet(workbook, sheetSelector);

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        if (lastRow == 0 || lastCol == 0)
        {
            return Task.FromResult(new SheetGrid([], []));
        }

        var letters = Enumerable.Range(1, lastCol).Select(n => XLHelper.GetColumnLetterFromNumber(n)).ToList();

        var rows = new List<IReadOnlyList<string>>(lastRow);
        for (var r = 1; r <= lastRow; r++)
        {
            var cells = new List<string>(lastCol);
            for (var c = 1; c <= lastCol; c++)
            {
                cells.Add(CellDisplay(sheet.Cell(r, c)));
            }

            rows.Add(cells);
        }

        return Task.FromResult(new SheetGrid(letters, rows));
    }

    private static IXLWorksheet SelectSheet(XLWorkbook workbook, string? selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            return workbook.Worksheet(1);
        }

        if (int.TryParse(selector, out var index) && index >= 1 && index <= workbook.Worksheets.Count)
        {
            return workbook.Worksheet(index);
        }

        return workbook.Worksheets.FirstOrDefault(w => w.Name == selector) ?? workbook.Worksheet(1);
    }

    private static string CellDisplay(IXLCell cell)
    {
        try
        {
            if (cell.HasFormula)
            {
                var value = cell.Value;   // forces ClosedXML to evaluate the formula
                return value.IsBlank ? string.Empty : cell.GetFormattedString();
            }

            return cell.GetFormattedString();
        }
        catch
        {
            return cell.HasFormula ? $"={cell.FormulaA1}" : string.Empty;
        }
    }
}