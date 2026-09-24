using Transit.Core.Declarations;
using Xunit;

namespace Transit.Tests.Declarations;

public class DeclarationReminderTests
{
    private static DeclarationRunSummary Run(int year, int month) =>
        new(1, year, month, DateTime.UtcNow, "Km", 3, true, true, 494.8m);

    [Fact]
    public void Pending_is_the_previous_month_when_it_has_no_run()
    {
        var pending = DeclarationReminder.Pending(new DateOnly(2026, 9, 1), [Run(2026, 7)]);

        Assert.NotNull(pending);
        Assert.Equal((2026, 8), (pending.Year, pending.Month));
        Assert.Equal("August 2026", pending.Label);
        Assert.Equal("2026-08", pending.Query);
    }

    [Fact]
    public void Pending_is_null_once_the_previous_month_is_declared()
        => Assert.Null(DeclarationReminder.Pending(new DateOnly(2026, 9, 24), [Run(2026, 8)]));

    [Fact]
    public void Pending_in_january_is_december_of_the_previous_year()
    {
        var pending = DeclarationReminder.Pending(new DateOnly(2027, 1, 4), [Run(2026, 11)]);
        Assert.Equal((2026, 12), (pending!.Year, pending.Month));
    }

    [Fact]
    public void A_run_for_the_current_month_does_not_declare_the_previous_one()
        => Assert.NotNull(DeclarationReminder.Pending(new DateOnly(2026, 9, 24), [Run(2026, 9)]));

    [Theory]
    [InlineData("2026-08", 2026, 8)]
    [InlineData("2027-01", 2027, 1)]
    public void TryParseQuery_reads_year_month(string value, int year, int month)
    {
        Assert.True(DeclarationReminder.TryParseQuery(value, out var y, out var m));
        Assert.Equal((year, month), (y, m));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2026-13")]
    [InlineData("2026-8")]
    [InlineData("08-2026")]
    [InlineData("2026-08-01")]
    public void TryParseQuery_rejects_anything_else(string? value)
        => Assert.False(DeclarationReminder.TryParseQuery(value, out _, out _));
}
