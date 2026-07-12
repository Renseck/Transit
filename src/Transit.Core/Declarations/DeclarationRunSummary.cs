namespace Transit.Core.Declarations;

// A read-model row for the history list, projected without materialising the run's rows.
// TravelDayCount is a projected count (travel rows), never a km total.
public sealed record DeclarationRunSummary(
    int Id,
    int PeriodYear,
    int PeriodMonth,
    DateTime CreatedUtc,
    string TemplateName,
    int TravelDayCount,
    bool HasXlsx,
    bool HasPdf);