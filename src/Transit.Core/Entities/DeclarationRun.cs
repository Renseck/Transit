namespace Transit.Core.Entities;

// A generated declaration: an append-only, immutable snapshot of the reviewed rows plus the paths
// to its artifacts on disk. Re-running a month creates a new run; runs are never overwritten.
public class DeclarationRun
{
    public int Id { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public int TemplateProfileId { get; set; }
    public required string TemplateName { get; set; }   // frozen copy of the profile name at generation
    public DateTime CreatedUtc { get; set; }
    public required string XlsxFilePath { get; set; }   // set after the artifact write
    public string? PdfFilePath { get; set; }            // null on an xlsx-only run (PDF conversion failed)

    public List<DeclarationRow> Rows { get; set; } = [];
}