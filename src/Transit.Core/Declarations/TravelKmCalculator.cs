using Transit.Core.Entities;
using Transit.Core.Routes;

namespace Transit.Core.Declarations;

/// <summary>
/// Pure month-total calculator for the /attendance counter, where there is no route picker.
/// Keeps only travel-marked days in the target month, resolves each destination's default route,
/// and sums <see cref="Route.TotalKm"/>. A travel day whose destination has no route contributes
/// 0 km but still counts as a travel day. No I/O — unit-testable.
/// </summary>
public static class TravelKmCalculator
{
    public static TravelKmSummary ForMarks(
        IReadOnlyDictionary<DateOnly, int> marks,
        int year,
        int month,
        IReadOnlyList<WorkLocation> locations,
        IReadOnlyList<Route> routes)
    {
        var total = 0m;
        var travelDays = 0;

        foreach (var (date, locationId) in marks)
        {
            if (date.Year != year || date.Month != month)
            {
                continue;
            }

            var location = locations.FirstOrDefault(l => l.Id == locationId);
            if (location is null || !location.CountsAsTravel)
            {
                continue;
            }

            travelDays++;
            total += RouteRules.ResolveDefaultFor(locationId, routes)?.TotalKm ?? 0m;
        }

        return new TravelKmSummary(total, travelDays);
    }
}