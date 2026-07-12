namespace Transit.Core.Entities;

// Maps a static header value onto a single cell (e.g. MonthName -> "G5").
public class CellMapping
{
    public int Id { get; set; }
    public int TemplateProfileId { get; set; }
    public required string CellRef { get; set; }  // "C5"
    public TemplateCellSource Source { get; set; }
    public string? Value { get; set; }            // literal for EmployeeName/PersonnelNumber/Custom
    public string? Format { get; set; }
}