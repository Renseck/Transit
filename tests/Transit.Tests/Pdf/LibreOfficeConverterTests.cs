using ClosedXML.Excel;
using Microsoft.Extensions.Options;
using Transit.Infrastructure.Configuration;
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

        var converter = new LibreOfficePdfConverter(OptionsWrapper());
        var pdf = await converter.ConvertToPdfAsync(TinyXlsx());

        Assert.NotEmpty(pdf);
        Assert.Equal("%PDF"u8.ToArray(), pdf.Take(4).ToArray());   // PDF magic bytes
    }

    private static IOptions<PdfConverterOptions> OptionsWrapper() => Microsoft.Extensions.Options.Options.Create(Options);
}