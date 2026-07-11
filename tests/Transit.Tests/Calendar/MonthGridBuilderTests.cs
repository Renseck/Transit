using Transit.Core.Calendar;
using Xunit;

namespace Transit.Tests.Calendar;

public class MonthGridBuilderTests
{
    [Fact]
    public void Build_weeks_are_monday_first_and_seven_wide()
    {
        var grid = MonthGridBuilder.Build(2026, 7);

        Assert.All(grid.Weeks, week => Assert.Equal(7, week.Count));
        Assert.Equal(DayOfWeek.Monday, grid.Weeks[0][0].Date.DayOfWeek);
    }

    [Fact]
    public void Build_covers_the_first_and_last_of_the_month_as_in_month()
    {
        var grid = MonthGridBuilder.Build(2026, 7);
        var days = grid.Weeks.SelectMany(w => w).ToList();

        Assert.Contains(days, d => d.Date == new DateOnly(2026, 7, 1) && d.InCurrentMonth);
        Assert.Contains(days, d => d.Date == new DateOnly(2026, 7, 31) && d.InCurrentMonth);
    }

    [Fact]
    public void Build_flags_leading_adjacent_days_as_out_of_month()
    {
        // 1 July 2026 is a Wednesday, so the grid leads with Mon 29 + Tue 30 June.
        var grid = MonthGridBuilder.Build(2026, 7);
        var first = grid.Weeks[0][0];

        Assert.Equal(new DateOnly(2026, 6, 29), first.Date);
        Assert.False(first.InCurrentMonth);
    }
}