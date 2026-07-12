using ClosedXML.Excel;
using Transit.Core.Declarations;
using Transit.Infrastructure.Excel;
using Xunit;

namespace Transit.Tests.Excel;

public class ClosedXmlSheetPreviewReaderTests
{
    private static byte[] SampleWorkbook()
    {
        using var wb = new XLWorkbook();
        var sheet = wb.AddWorksheet("Data");
        sheet.Cell("A1").Value = "Naam";
        sheet.Cell("B1").Value = "Rens";
        sheet.Cell("A2").Value = 10;
        sheet.Cell("A3").Value = 20;
        sheet.Cell("A4").FormulaA1 = "SUM(A2:A3)";
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static string Cell(SheetGrid grid, string columnLetter, int rowNumber)
    {
        var col = grid.ColumnLetters.ToList().IndexOf(columnLetter);
        return grid.Rows[rowNumber - 1][col];
    }

    [Fact]
    public async Task Reads_values_and_evaluates_a_formula()
    {
        var reader = new ClosedXmlSheetPreviewReader();
        var grid = await reader.ReadAsync(new MemoryStream(SampleWorkbook()), sheetSelector: null);

        Assert.Equal(["A", "B"], grid.ColumnLetters);
        Assert.Equal(4, grid.Rows.Count);              // used range rows 1..4
        Assert.Equal("Rens", Cell(grid, "B", 1));      // text cell
        Assert.Equal("30", Cell(grid, "A", 4));        // SUM evaluated, not "=SUM(A2:A3)"
    }

    [Fact]
    public async Task Empty_sheet_yields_an_empty_grid()
    {
        using var wb = new XLWorkbook();
        wb.AddWorksheet("Blank");
        using var ms = new MemoryStream();
        wb.SaveAs(ms);

        var reader = new ClosedXmlSheetPreviewReader();
        var grid = await reader.ReadAsync(new MemoryStream(ms.ToArray()), sheetSelector: null);

        Assert.Empty(grid.ColumnLetters);
        Assert.Empty(grid.Rows);
    }
}