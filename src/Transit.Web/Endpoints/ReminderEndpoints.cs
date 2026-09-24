using System.Net;
using Microsoft.AspNetCore.Mvc;
using Transit.Core.Abstractions;
using Transit.Core.Attendance;
using Transit.Core.Declarations;
using Transit.Core.Services;

namespace Transit.Web.Endpoints;

public static class ReminderEndpoints
{
    public static void MapReminderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reminder/today-status", GetTodayStatusAsync);
        app.MapPost("/api/reminder/log-today", LogTodayAsync);
    }

    public static async Task<TodayStatus> GetTodayStatusAsync(
        IAttendanceStore store, LocationService locations, DeclarationHistoryService history,
        [FromQuery(Name = "network")] string[]? networks, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var entries = await store.GetRangeAsync(today, today, ct);
        var active = await locations.GetActiveAsync(ct);

        var match = NetworkHintMatcher.Match(active, networks ?? []);
        var suggestion = match is null
            ? null
            : new ReminderSuggestion(match.Location.Id, match.Location.Name, match.Network);
        var options = ReminderChoices.Build(active, match?.Location)
            .Select(l => new ReminderOption(l.Id, l.Name))
            .ToList();

        var pending = DeclarationReminder.Pending(today, await history.GetSummariesAsync(ct));

        return new TodayStatus(today, AttendanceStatus.IsExplicitlyLogged(entries, today), suggestion, options, pending);
    }

    // One-click log from the toast: loopback only, today only, never overwrites (QuickLogRules).
    public static async Task<IResult> LogTodayAsync(
        LogTodayRequest request, HttpContext http, IAttendanceStore store, LocationService locations, CancellationToken ct)
    {
        if (!IsLoopback(http.Connection.RemoteIpAddress))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var all = await locations.GetAllAsync(includeArchived: true, ct);
        var location = all.FirstOrDefault(l => l.Id == request.LocationId);
        var entries = await store.GetRangeAsync(today, today, ct);

        var outcome = QuickLogRules.Evaluate(today, request.Date, location, entries);
        var name = outcome.ToString();

        if (outcome == QuickLogOutcome.StaleReminder)
        {
            return Results.Conflict(new LogTodayResponse(name, request.Date, null));
        }

        if (outcome == QuickLogOutcome.UnknownLocation)
        {
            return Results.NotFound(new LogTodayResponse(name, null, null));
        }

        if (outcome == QuickLogOutcome.AlreadyLogged)
        {
            var existingId = entries.First(e => e.Date == today).WorkLocationId;
            var existingName = all.FirstOrDefault(l => l.Id == existingId)?.Name;
            return Results.Conflict(new LogTodayResponse(name, today, existingName));
        }

        await store.SetAsync(today, location!.Id, ct);
        return Results.Ok(new LogTodayResponse(name, today, location.Name));
    }

    private static bool IsLoopback(IPAddress? remote)
    {
        if (remote is null)
        {
            return false;
        }

        var address = remote.IsIPv4MappedToIPv6 ? remote.MapToIPv4() : remote;
        return IPAddress.IsLoopback(address);
    }
}