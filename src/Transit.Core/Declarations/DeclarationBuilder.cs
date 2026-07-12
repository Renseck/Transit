using Transit.Core.Entities;
using Transit.Core.Routes;
using Transit.Core.Services;

namespace Transit.Core.Declarations;

/// <summary>
/// Pure declaration compute. Materialises every weekday of the month plus any explicitly-marked
/// weekend day, defaults unmarked weekdays to the IsDefaultForUnmarked location, resolves each
/// travel day's route by destination, and computes per-day km. No I/O; no monthly total (the
/// employer template owns that).
/// </summary>
public static class DeclarationBuilder
{
    public static DeclarationDraft Build(
        int year, int month,
        IReadOnlyList<AttendanceEntry> entries,
        IReadOnlyList<WorkLocation> locations,   // includes archived, for mark resolution
        IReadOnlyList<Route> routes)
    {
        var defaultLocation = LocationService.ResolveDefault(locations.Where(l => !l.IsArchived).ToList());
        var markByDate = entries.ToDictionary(e => e.Date, e => e.WorkLocationId);
        var locationById = locations.ToDictionary(l => l.Id);

        var rows = new List<DeclarationDraftRow>();
        var daysInMonth = DateTime.DaysInMonth(year, month);
        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var hasEntry = markByDate.TryGetValue(date, out var markedId);

            if (isWeekend && !hasEntry)
            {
                continue;   // unmarked weekends never appear
            }

            var (location, isUnmarkedDefault) = ResolveLocation(hasEntry, markedId, locationById, defaultLocation);
            rows.Add(BuildRow(date, location, isUnmarkedDefault, routes));
        }

        return new DeclarationDraft(year, month, rows);
    }

    private static (WorkLocation location, bool isUnmarkedDefault) ResolveLocation(
        bool hasEntry, int markedId, IReadOnlyDictionary<int, WorkLocation> locationById, WorkLocation defaultLocation)
    {
        if (!hasEntry)
        {
            return (defaultLocation, true);
        }

        // A mark whose location vanished entirely falls back to the default, still counted explicit.
        return locationById.TryGetValue(markedId, out var loc) ? (loc, false) : (defaultLocation, false);
    }

    private static DeclarationDraftRow BuildRow(
        DateOnly date, WorkLocation location, bool isUnmarkedDefault, IReadOnlyList<Route> routes)
    {
        var weekday = date.ToString("dddd");

        if (!location.CountsAsTravel)
        {
            return new DeclarationDraftRow(
                date, weekday, location.Id, location.Name, location.ColorHex, location.CountsAsTravel,
                isUnmarkedDefault, null, null, 0m, 0m, 0m, false);
        }

        var route = RouteRules.ResolveDefaultFor(location.Id, routes);
        if (route is null)
        {
            return new DeclarationDraftRow(
                date, weekday, location.Id, location.Name, location.ColorHex, location.CountsAsTravel,
                isUnmarkedDefault, null, null, 0m, 0m, 0m, true);
        }

        return new DeclarationDraftRow(
            date, weekday, location.Id, location.Name, location.ColorHex, location.CountsAsTravel,
            isUnmarkedDefault, route.Id, route.Name, route.OutboundKm, route.ReturnKm, route.TotalKm, false);
    }
}