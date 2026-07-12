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
            rows.Add(BuildRow(date, location, isUnmarkedDefault, routes, locationById));
        }

        return new DeclarationDraft(year, month, rows);
    }

    /// <summary>
    /// Re-resolves the review grid's per-day route overrides onto an already-built draft so the
    /// result equals what the user reviewed. Only travel rows whose date is in <paramref name="selections"/>
    /// change; a null selection or an unknown route id yields a no-route row. Pure; no I/O.
    /// </summary>
    public static DeclarationDraft ApplySelections(
        DeclarationDraft draft,
        IReadOnlyDictionary<DateOnly, int?> selections,
        IReadOnlyList<Route> routes,
        IReadOnlyList<WorkLocation> locations)
    {
        var routeById = routes.ToDictionary(r => r.Id);
        var locationById = locations.ToDictionary(l => l.Id);
        var rows = draft.Rows.Select(row => ApplyToRow(row, selections, routeById, locationById)).ToList();
        return new DeclarationDraft(draft.Year, draft.Month, rows);
    }

    private static DeclarationDraftRow ApplyToRow(
        DeclarationDraftRow row,
        IReadOnlyDictionary<DateOnly, int?> selections,
        IReadOnlyDictionary<int, Route> routeById,
        IReadOnlyDictionary<int, WorkLocation> locationById)
    {
        if (!row.CountsAsTravel || !selections.TryGetValue(row.Date, out var routeId))
        {
            return row;
        }

        if (routeId is null || !routeById.TryGetValue(routeId.Value, out var route))
        {
            return row with
            {
                SelectedRouteId = null, RouteName = null,
                FromLocationName = null, FromPostcode = null, ToLocationName = null, ToPostcode = null,
                OutboundKm = 0m, ReturnKm = 0m, Kilometers = 0m, HasNoRoute = true,
            };
        }

        var from = locationById.GetValueOrDefault(route.FromLocationId);
        var to = locationById.GetValueOrDefault(route.ToLocationId);

        return row with
        {
            SelectedRouteId = route.Id, RouteName = route.Name,
            FromLocationName = from?.Name, FromPostcode = from?.Postcode,
            ToLocationName = to?.Name, ToPostcode = to?.Postcode,
            OutboundKm = route.OutboundKm, ReturnKm = route.ReturnKm, Kilometers = route.TotalKm, HasNoRoute = false,
        };
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
        DateOnly date, WorkLocation location, bool isUnmarkedDefault, IReadOnlyList<Route> routes,
        IReadOnlyDictionary<int, WorkLocation> locationById)
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

        var from = locationById.GetValueOrDefault(route.FromLocationId);
        var to = locationById.GetValueOrDefault(route.ToLocationId);

        return new DeclarationDraftRow(
            date, weekday, location.Id, location.Name, location.ColorHex, location.CountsAsTravel,
            isUnmarkedDefault, route.Id, route.Name, route.OutboundKm, route.ReturnKm, route.TotalKm, false,
            FromLocationName: from?.Name, FromPostcode: from?.Postcode,
            ToLocationName: to?.Name, ToPostcode: to?.Postcode);
    }
}