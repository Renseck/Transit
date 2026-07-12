namespace Transit.Core.Entities;

// A versioned employer-template configuration. One is active. The .xlsx file lives on disk
// at TemplateFilePath; the mappings describe how declaration data fills it.
public class TemplateProfile
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string TemplateFilePath { get; set; }
    public required string SheetSelector { get; set; }  // sheet name or "1"-based index string
    public int DataStartRow { get; set; }               // first repeating-data row (e.g. 8)
    public int DataRowCount { get; set; }                // physical data rows present (e.g. 15)
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }

    public List<ColumnMapping> ColumnMappings { get; set; } = [];
    public List<CellMapping> CellMappings { get; set; } = [];
}