using Transit.Core.Entities;

namespace Transit.Core.Locations;

/// <summary>
/// Pure rules for work-location management. No I/O — every method decides from an
/// in-memory snapshot so the invariants are unit-testable. <c>LocationService</c>
/// loads the snapshot, calls these, then persists via <c>ILocationStore</c>.
/// Failures throw <see cref="InvalidOperationException"/> with a user-facing message.
/// </summary>
public static class LocationRules
{
    public static string NormalizeName(string? name) => (name ?? string.Empty).Trim();

    public static void ValidateNewOrRenamed(
        string name, string colorHex, IReadOnlyList<WorkLocation> activeLocations, int? editingId = null)
    {
        var trimmed = NormalizeName(name);
        if (trimmed.Length == 0)
        {
            throw new InvalidOperationException("A location needs a name.");
        }

        var clash = activeLocations.Any(l =>
            l.Id != editingId &&
            string.Equals(l.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (clash)
        {
            throw new InvalidOperationException($"A location named \"{trimmed}\" already exists.");
        }

        if (!LinePalette.Contains(colorHex))
        {
            throw new InvalidOperationException("Pick a colour from the line palette.");
        }
    }

    public static void EnsureCanArchive(int locationId, IReadOnlyList<WorkLocation> allLocations)
    {
        var target = allLocations.FirstOrDefault(l => l.Id == locationId)
            ?? throw new InvalidOperationException("That location no longer exists.");

        if (target.IsDefaultForUnmarked)
        {
            throw new InvalidOperationException(
                "Reassign the default to another line before archiving this one.");
        }

        var activeCount = allLocations.Count(l => !l.IsArchived);
        if (activeCount <= 1)
        {
            throw new InvalidOperationException("At least one active location is required.");
        }
    }

    public static void EnsureCanBeDefault(int locationId, IReadOnlyList<WorkLocation> allLocations)
    {
        var target = allLocations.FirstOrDefault(l => l.Id == locationId)
            ?? throw new InvalidOperationException("That location no longer exists.");

        if (target.IsArchived)
        {
            throw new InvalidOperationException("Unarchive the location before making it the default.");
        }
    }

    public static void ValidateReorder(IReadOnlyList<int> orderedIds, IReadOnlyList<WorkLocation> knownLocations)
    {
        var known = knownLocations.Select(l => l.Id).ToHashSet();
        foreach (var id in orderedIds)
        {
            if (!known.Contains(id))
            {
                throw new InvalidOperationException($"Unknown location id {id} in reorder.");
            }
        }
    }
}