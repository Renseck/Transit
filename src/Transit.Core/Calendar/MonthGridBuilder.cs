namespace Transit.Core.Calendar;

public static class MonthGridBuilder
{
    // Monday-first weeks spanning the whole month, padded with adjacent-month days.
    public static MonthGrid Build(int year, int month)
    {
        var first = new DateOnly(year, month, 1);
        var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        var gridStart = first.AddDays(-MondayOffset(first));
        var gridEnd = last.AddDays(6 - MondayOffset(last));

        var weeks = new List<IReadOnlyList<CalendarDay>>();
        for (var cursor = gridStart; cursor <= gridEnd; cursor = cursor.AddDays(7))
        {
            var week = new List<CalendarDay>(7);
            for (var i = 0; i < 7; i++)
            {
                var date = cursor.AddDays(i);
                week.Add(new CalendarDay(date, date.Month == month && date.Year == year));
            }
            weeks.Add(week);
        }

        return new MonthGrid(year, month, weeks);
    }

    // 0 for Monday … 6 for Sunday.
    private static int MondayOffset(DateOnly date) => ((int)date.DayOfWeek + 6) % 7;
}