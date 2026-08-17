namespace Transit.Core.Declarations;

/// <summary>
/// Pure grouping for the /history year-grid: projects run summaries into twelve month tiles for a
/// year (declared? newest run, run count), and lists a single month's runs newest-first for the
/// drill-in modal. No I/O — unit-testable.
/// </summary>
public static class HistoryCalendar
{
    public static IReadOnlyList<MonthTileState> BuildYear(
        IReadOnlyList<DeclarationRunSummary> runs, int year)
    {
        var tiles = new List<MonthTileState>(12);
        for (var month = 1; month <= 12; month++)
        {
            var forMonth = runs.Where(r => r.PeriodYear == year && r.PeriodMonth == month).ToList();
            var latest = forMonth.OrderByDescending(r => r.CreatedUtc).FirstOrDefault();
            tiles.Add(new MonthTileState(month, latest is not null, latest, forMonth.Count));
        }

        return tiles;
    }

    public static IReadOnlyList<DeclarationRunSummary> RunsForMonth(
        IReadOnlyList<DeclarationRunSummary> runs, int year, int month)
        => runs.Where(r => r.PeriodYear == year && r.PeriodMonth == month)
               .OrderByDescending(r => r.CreatedUtc)
               .ToList();
}