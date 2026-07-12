namespace Transit.Core.Entities;

// The source of a static header-cell value.
public enum TemplateCellSource
{
    MonthName,        // derived from the declaration period (nl-NL)
    Year,             // derived from the declaration period
    EmployeeName,     // static literal in Value
    PersonnelNumber,  // static literal in Value
    Custom,           // static literal in Value
}