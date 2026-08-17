using Transit.Core.Declarations;
using Xunit;

namespace Transit.Tests.Declarations;

public class HistoryCalendarTests
{
    private static DeclarationRunSummary Run(int id, int year, int month, DateTime created)
        => new(id, year, month, created, "Km", TravelDayCount: 3, HasXlsx: true, HasPdf: true, TotalKm: 60m);

    [Fact]
    public void BuildYear_returns_twelve_tiles_in_month_order()
    {
        var tiles = HistoryCalendar.BuildYear([], 2026);

        Assert.Equal(12, tiles.Count);
        Assert.Equal(1, tiles[0].Month);
        Assert.Equal(12, tiles[11].Month);
        Assert.All(tiles, t => Assert.False(t.IsDeclared));
    }

    [Fact]
    public void BuildYear_marks_declared_months_with_their_newest_run()
    {
        var runs = new[]
        {
            Run(1, 2026, 8, new DateTime(2026, 8, 10)),
            Run(2, 2026, 8, new DateTime(2026, 8, 20)),   // newer for August
            Run(3, 2026, 3, new DateTime(2026, 3, 5)),
        };

        var tiles = HistoryCalendar.BuildYear(runs, 2026);

        var august = tiles.Single(t => t.Month == 8);
        Assert.True(august.IsDeclared);
        Assert.Equal(2, august.LatestRun!.Id);   // newest
        Assert.Equal(2, august.RunCount);

        Assert.True(tiles.Single(t => t.Month == 3).IsDeclared);
        Assert.False(tiles.Single(t => t.Month == 4).IsDeclared);
    }

    [Fact]
    public void BuildYear_ignores_other_years()
    {
        var runs = new[] { Run(1, 2025, 8, new DateTime(2025, 8, 10)) };

        var tiles = HistoryCalendar.BuildYear(runs, 2026);

        Assert.All(tiles, t => Assert.False(t.IsDeclared));
    }

    [Fact]
    public void RunsForMonth_returns_that_months_runs_newest_first()
    {
        var runs = new[]
        {
            Run(1, 2026, 8, new DateTime(2026, 8, 10)),
            Run(2, 2026, 8, new DateTime(2026, 8, 20)),
            Run(3, 2026, 9, new DateTime(2026, 9, 1)),
        };

        var forAug = HistoryCalendar.RunsForMonth(runs, 2026, 8);

        Assert.Equal([2, 1], forAug.Select(r => r.Id));
    }
}