using Transit.Core.Entities;

namespace Transit.Core.Attendance;

/// <summary>
/// Pure decision for a one-click log from the reminder toast. Only today may be logged, only to an
/// active location, and an explicit mark is never overwritten. Order matters: a stale toast is
/// reported as stale even if its location has since been archived.
/// </summary>
public static class QuickLogRules
{
    public static QuickLogOutcome Evaluate(
        DateOnly today, DateOnly requestedDate, WorkLocation? location, IReadOnlyList<AttendanceEntry> todayEntries)
    {
        if (requestedDate != today)
        {
            return QuickLogOutcome.StaleReminder;
        }

        if (location is null || location.IsArchived)
        {
            return QuickLogOutcome.UnknownLocation;
        }

        if (AttendanceStatus.IsExplicitlyLogged(todayEntries, today))
        {
            return QuickLogOutcome.AlreadyLogged;
        }

        return QuickLogOutcome.Logged;
    }
}
