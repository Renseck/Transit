namespace Transit.Infrastructure.Configuration;

public sealed class PdfConverterOptions
{
    public const string SectionName = "Transit:Pdf";

    // Full path to the LibreOffice CLI. Overridable via configuration.
    public string SofficePath { get; set; } = @"C:\Program Files\LibreOffice\program\soffice.exe";

    // Hard ceiling on a single conversion before it is abandoned.
    public int TimeoutSeconds { get; set; } = 120;
}