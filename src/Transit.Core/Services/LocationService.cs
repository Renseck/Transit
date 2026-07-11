using Transit.Core.Abstractions;
using Transit.Core.Entities;

namespace Transit.Core.Services;

public sealed class LocationService
{
    private readonly ILocationStore _store;

    public LocationService(ILocationStore store) => _store = store;

    public Task<IReadOnlyList<WorkLocation>> GetAllAsync(CancellationToken ct = default)
        => _store.GetAllAsync(ct);

    // Exactly one location must be the default for unmarked days.
    public static WorkLocation ResolveDefault(IReadOnlyList<WorkLocation> locations)
    {
        var defaults = locations.Where(l => l.IsDefaultForUnmarked).ToList();
        if (defaults.Count != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one default-for-unmarked location, found {defaults.Count}.");
        }

        return defaults[0];
    }
}