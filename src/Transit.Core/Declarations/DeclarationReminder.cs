using System.Globalization;

namespace Transit.Core.Declarations;

/// <summary>
/// Pure "is last month still undeclared?" rule, shared by the overview nudge and the desktop toast,
/// plus the yyyy-MM month query used to deep-link /declaration. Only the previous month is nudged:
/// a completed month is due from its first day after; older gaps stay visible on /history.
/// </summary>
public static class DeclarationReminder
{
    private const string QueryFormat = "yyyy-MM";

    public static PendingDeclaration? Pending(DateOnly today, IEnumerable<DeclarationRunSummary> runs)
    {
        var previous = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        if (runs.Any(r => r.PeriodYear == previous.Year && r.PeriodMonth == previous.Month))
        {
            return null;
        }

        return new PendingDeclaration(
            previous.Year,
            previous.Month,
            previous.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
            previous.ToString(QueryFormat, CultureInfo.InvariantCulture));
    }

    public static bool TryParseQuery(string? value, out int year, out int month)
    {
        year = 0;
        month = 0;
        if (!DateOnly.TryParseExact(value, QueryFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return false;
        }

        (year, month) = (date.Year, date.Month);
        return true;
    }
}
