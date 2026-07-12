using ClosedXML.Excel;
using Transit.Core.Declarations;
using Transit.Core.Entities;
using Transit.Infrastructure.Excel;
using Xunit;

namespace Transit.Tests.Excel;

public class ClosedXmlTemplateWriterTests
{
    private static readonly string FixturePath =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "km-template.xlsx");

    // Mirrors the real template: Blad1, header row 7, data rows 8..22 (15), SUM(F8:F22).
    private static TemplateProfile Profile() => new()
    {
        Id = 1, Name = "Km", TemplateFilePath = "x", SheetSelector = "Blad1",
        DataStartRow = 8, DataRowCount = 15, IsActive = true, CreatedUtc = DateTime.UtcNow,
        ColumnMappings =
        [
            new ColumnMapping { Field = TemplateColumnField.Date, Column = "A", Format = "dd-MM-yyyy" },
            new ColumnMapping { Field = TemplateColumnField.FromLocation, Column = "B" },
            new ColumnMapping { Field = TemplateColumnField.FromPostcode, Column = "C" },
            new ColumnMapping { Field = TemplateColumnField.ToLocation, Column = "D" },
            new ColumnMapping { Field = TemplateColumnField.ToPostcode, Column = "E" },
            new ColumnMapping { Field = TemplateColumnField.Kilometers, Column = "F", Format = "0.0" },
        ],
        CellMappings =
        [
            new CellMapping { CellRef = "C5", Source = TemplateCellSource.EmployeeName, Value = "Rens van Eck" },
            new CellMapping { CellRef = "G5", Source = TemplateCellSource.MonthName },
        ],
    };

    private static DeclarationDraftRow Travel(DateOnly date, decimal km) => new(
        date, date.ToString("dddd"), 2, "Office", "#3B5BDB", true, false,
        10, "Home → Office", km / 2, km / 2, km, false,
        "Home", "1234 AB", "Office", "5678 CD");

    private static DeclarationDraftRow NonTravel(DateOnly date) => new(
        date, date.ToString("dddd"), 1, "Home", "#4C566A", false, true,
        null, null, 0m, 0m, 0m, false);

    private static byte[] Fill(TemplateProfile profile, DeclarationDraft draft)
    {
        using var stream = File.OpenRead(FixturePath);
        return new ClosedXmlTemplateWriter().FillAsync(profile, stream, draft).GetAwaiter().GetResult();
    }

    [Fact]
    public void Writes_travel_rows_header_cells_and_leaves_formulas_intact()
    {
        var draft = new DeclarationDraft(2026, 7,
        [
            Travel(new DateOnly(2026, 7, 1), 21.8m),
            NonTravel(new DateOnly(2026, 7, 2)),   // must NOT reach the sheet
            Travel(new DateOnly(2026, 7, 3), 21.8m),
        ]);

        using var wb = new XLWorkbook(new MemoryStream(Fill(Profile(), draft)));
        var sheet = wb.Worksheet("Blad1");

        Assert.Equal("Home", sheet.Cell("B8").GetString());
        Assert.Equal("1234 AB", sheet.Cell("C8").GetString());
        Assert.Equal(21.8, sheet.Cell("F8").GetDouble(), 3);
        Assert.Equal(21.8, sheet.Cell("F9").GetDouble(), 3);   // second travel day, non-travel skipped
        Assert.Equal("Rens van Eck", sheet.Cell("C5").GetString());
        Assert.Equal("juli", sheet.Cell("G5").GetString());
        Assert.Equal("SUM(F8:F22)", sheet.Cell("F24").FormulaA1);   // template total untouched
        Assert.True(sheet.Cell("F10").IsEmpty());                   // surplus rows left blank
    }

    [Fact]
    public void Inserts_rows_and_expands_the_sum_for_a_heavy_month()
    {
        var rows = Enumerable.Range(1, 18)   // 18 travel days > 15 capacity
            .Select(d => Travel(new DateOnly(2026, 7, 1).AddDays(d - 1), 20m))
            .Cast<DeclarationDraftRow>()
            .ToList();
        var draft = new DeclarationDraft(2026, 7, rows);

        using var wb = new XLWorkbook(new MemoryStream(Fill(Profile(), draft)));
        var sheet = wb.Worksheet("Blad1");

        Assert.Equal(20.0, sheet.Cell("F25").GetDouble(), 3);       // 18th data row now populated (8..25)
        Assert.Equal("SUM(F8:F25)", sheet.Cell("F27").FormulaA1);   // SUM expanded; total shifted down by 3
    }
}