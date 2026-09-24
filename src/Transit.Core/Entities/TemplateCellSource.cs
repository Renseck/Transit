namespace Transit.Core.Entities;

// The source of a static header-cell value.
public enum TemplateCellSource
{
    MonthName,        // derived from the declaration period (nl-NL)
    Year,             // derived from the declaration period
    EmployeeName,     // static literal in Value
    PersonnelNumber,  // static literal in Value
    Custom,           // static literal in Value
    SignatureName,    // signed output only: literal in Value, else the EmployeeName literal
    SignatureDate,    // signed output only: the signing date, formatted per Format (default dd/MM/yyyy)
}