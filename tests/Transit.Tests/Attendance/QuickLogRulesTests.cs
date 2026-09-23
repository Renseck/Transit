using Transit.Core.Attendance;
using Transit.Core.Entities;
using Xunit;

namespace Transit.Tests.Attendance;

public class QuickLogRulesTests
{
    private static readonly DateOnly Today = new(2026, 9, 23);

    private static WorkLocation Office(bool archived = false) => new()
    {
        Id = 3, Name = "Office", ColorHex = "#000000", CountsAsTravel = true, IsArchived = archived,
    };

    [Fact]
    public void Unlogged_today_with_an_active_location_is_logged()
    {
        var outcome = QuickLogRules.Evaluate(Today, Today, Office(), []);

        Assert.Equal(QuickLogOutcome.Logged, outcome);
    }

    [Fact]
    public void A_request_for_another_date_is_a_stale_reminder()
    {
        var outcome = QuickLogRules.Evaluate(Today, Today.AddDays(-1), Office(), []);

        Assert.Equal(QuickLogOutcome.StaleReminder, outcome);
    }

    [Fact]
    public void A_missing_location_is_unknown()
    {
        var outcome = QuickLogRules.Evaluate(Today, Today, null, []);

        Assert.Equal(QuickLogOutcome.UnknownLocation, outcome);
    }

    [Fact]
    public void An_archived_location_is_unknown()
    {
        var outcome = QuickLogRules.Evaluate(Today, Today, Office(archived: true), []);

        Assert.Equal(QuickLogOutcome.UnknownLocation, outcome);
    }

    [Fact]
    public void An_explicit_entry_for_today_is_already_logged()
    {
        var entries = new[] { new AttendanceEntry { Date = Today, WorkLocationId = 1 } };

        var outcome = QuickLogRules.Evaluate(Today, Today, Office(), entries);

        Assert.Equal(QuickLogOutcome.AlreadyLogged, outcome);
    }

    [Fact]
    public void Stale_is_reported_before_unknown_location()
    {
        var outcome = QuickLogRules.Evaluate(Today, Today.AddDays(-1), null, []);

        Assert.Equal(QuickLogOutcome.StaleReminder, outcome);
    }
}
