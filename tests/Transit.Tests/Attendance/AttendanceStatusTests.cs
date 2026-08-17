using Transit.Core.Attendance;
using Transit.Core.Entities;
using Xunit;

namespace Transit.Tests.Attendance;

public class AttendanceStatusTests
{
    private static readonly DateOnly Today = new(2026, 8, 17);

    [Fact]
    public void Explicit_entry_for_the_date_counts_as_logged()
    {
        var entries = new[] { new AttendanceEntry { Date = Today, WorkLocationId = 2 } };
        Assert.True(AttendanceStatus.IsExplicitlyLogged(entries, Today));
    }

    [Fact]
    public void No_entry_for_the_date_is_not_logged()
    {
        var entries = Array.Empty<AttendanceEntry>();
        Assert.False(AttendanceStatus.IsExplicitlyLogged(entries, Today));
    }

    [Fact]
    public void An_entry_only_for_another_date_is_not_logged()
    {
        var entries = new[] { new AttendanceEntry { Date = new DateOnly(2026, 8, 16), WorkLocationId = 2 } };
        Assert.False(AttendanceStatus.IsExplicitlyLogged(entries, Today));
    }
}