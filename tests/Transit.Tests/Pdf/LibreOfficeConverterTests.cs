using ClosedXML.Excel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Transit.Core.Declarations;
using Transit.Core.Entities;
using Transit.Infrastructure.Configuration;
using Transit.Infrastructure.Excel;
using Transit.Infrastructure.Pdf;
using Xunit;

namespace Transit.Tests.Pdf;

public class LibreOfficePdfConverterTests
{
    private static readonly PdfConverterOptions Options = new();

    private static byte[] TinyXlsx()
    {
        using var wb = new XLWorkbook();
        var sheet = wb.AddWorksheet("Sheet1");
        sheet.Cell("A1").Value = "hello";
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    [SkippableFact]
    public async Task Converts_xlsx_to_a_non_empty_pdf()
    {
        Skip.IfNot(File.Exists(Options.SofficePath), "LibreOffice not installed on this machine.");

        var converter = new LibreOfficePdfConverter(OptionsWrapper(), NullLogger<LibreOfficePdfConverter>.Instance);
        var pdf = await converter.ConvertToPdfAsync(TinyXlsx());

        Assert.NotEmpty(pdf);
        Assert.Equal("%PDF"u8.ToArray(), pdf.Take(4).ToArray());   // PDF magic bytes
    }

    // LibreOffice silently substitutes a missing font, so assert the handwriting font really got embedded.
    [SkippableFact]
    public async Task Signed_workbook_embeds_the_signature_font_in_the_pdf()
    {
        Skip.IfNot(File.Exists(Options.SofficePath), "LibreOffice not installed on this machine.");

        var profile = new TemplateProfile
        {
            Id = 1, Name = "Km", TemplateFilePath = "x", SheetSelector = "Blad1",
            DataStartRow = 8, DataRowCount = 15, IsActive = true, CreatedUtc = DateTime.UtcNow,
            CellMappings =
            [
                new CellMapping { CellRef = "C5", Source = TemplateCellSource.EmployeeName, Value = "Rens van Eck" },
                new CellMapping { CellRef = "B30", Source = TemplateCellSource.SignatureName },
            ],
        };
        var writer = new ClosedXmlTemplateWriter(Microsoft.Extensions.Options.Options.Create(new SignatureOptions()));
        await using var template = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "km-template.xlsx"));
        var xlsx = await writer.FillAsync(profile, template, new DeclarationDraft(2026, 8, []), new DateOnly(2026, 9, 2));

        var converter = new LibreOfficePdfConverter(OptionsWrapper(), NullLogger<LibreOfficePdfConverter>.Instance);
        var pdf = System.Text.Encoding.Latin1.GetString(await converter.ConvertToPdfAsync(xlsx));

        Assert.Contains("SegoeScript", pdf);   // BaseFont name of the embedded subset
    }

    private static IOptions<PdfConverterOptions> OptionsWrapper() => Microsoft.Extensions.Options.Options.Create(Options);
}