using Transit.Core.Abstractions;
using Transit.Core.Attendance;

namespace Transit.Web.Endpoints;

/// <summary>Loopback status used by the desktop reminder helper.</summary>
public sealed record TodayStatus(DateOnly Date, bool ExplicitlyLogged);

public static class ReminderEndpoints
{
    public static void MapReminderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reminder/today-status", GetTodayStatusAsync);
    }

    public static async Task<TodayStatus> GetTodayStatusAsync(
        IAttendanceStore store, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var entries = await store.GetRangeAsync(today, today, ct);
        return new TodayStatus(today, AttendanceStatus.IsExplicitlyLogged(entries, today));
    }
}