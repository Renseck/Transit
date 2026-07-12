using Transit.Core.Entities;

namespace Transit.Core.Routes;

/// <summary>
/// Pure rules for route management. No I/O — every method decides from an in-memory
/// snapshot so the invariants are unit-testable. <c>RouteService</c> loads the snapshot,
/// calls these, then persists via <c>IRouteStore</c>. Failures throw
/// <see cref="InvalidOperationException"/> with a user-facing message.
/// </summary>
public static class RouteRules
{
    public static string NormalizeName(string? name) => (name ?? string.Empty).Trim();

    public static void ValidateNewOrRenamed(
        string name, int fromId, int toId, decimal outboundKm, decimal returnKm,
        IReadOnlyList<Route> existingRoutes, IReadOnlyList<WorkLocation> activeLocations, int? editingId = null)
    {
        var trimmed = NormalizeName(name);
        if (trimmed.Length == 0)
        {
            throw new InvalidOperationException("A route needs a name.");
        }

        var clash = existingRoutes.Any(r =>
            r.Id != editingId &&
            string.Equals(r.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (clash)
        {
            throw new InvalidOperationException($"A route named \"{trimmed}\" already exists.");
        }

        if (fromId == toId)
        {
            throw new InvalidOperationException("A route must connect two different locations.");
        }

        if (activeLocations.All(l => l.Id != fromId))
        {
            throw new InvalidOperationException("The origin location is not available.");
        }

        if (activeLocations.All(l => l.Id != toId))
        {
            throw new InvalidOperationException("The destination location is not available.");
        }

        if (outboundKm < 0 || returnKm < 0)
        {
            throw new InvalidOperationException("Distances cannot be negative.");
        }

        if (outboundKm + returnKm <= 0)
        {
            throw new InvalidOperationException("A route needs a non-zero distance.");
        }
    }

    // The default route to a destination: the flagged one, else the sole route, else null.
    public static Route? ResolveDefaultFor(int toLocationId, IReadOnlyList<Route> routes)
    {
        var candidates = routes.Where(r => r.ToLocationId == toLocationId).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates.FirstOrDefault(r => r.IsDefault) ?? candidates[0];
    }

    // Which sibling should become the destination default after a route is deleted (or null).
    // Input is the full pre-delete snapshot (still containing the route being deleted).
    public static Route? PickPromotionAfterDelete(int deletedRouteId, IReadOnlyList<Route> routes)
    {
        var deleted = routes.FirstOrDefault(r => r.Id == deletedRouteId);
        if (deleted is null)
        {
            return null;
        }

        return routes.FirstOrDefault(r => r.Id != deletedRouteId && r.ToLocationId == deleted.ToLocationId);
    }
}