namespace Transit.Core.Entities;

// Maps a declaration field onto a repeating-row column (e.g. Kilometers -> "F").
public class ColumnMapping
{
    public int Id { get; set; }
    public int TemplateProfileId { get; set; }
    public TemplateColumnField Field { get; set; }
    public required string Column { get; set; }   // "A".."Z"
    public string? Format { get; set; }           // e.g. "dd-MM-yyyy", "0.0"
}