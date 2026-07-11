using Transit.Core.Abstractions;
using Transit.Core.Entities;
using Transit.Core.Locations;

namespace Transit.Core.Services;

public sealed class LocationService
{
    private readonly ILocationStore _store;

    public LocationService(ILocationStore store) => _store = store;

    public Task<IReadOnlyList<WorkLocation>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default)
        => _store.GetAllAsync(includeArchived, ct);

    public Task<IReadOnlyList<WorkLocation>> GetActiveAsync(CancellationToken ct = default)
        => _store.GetAllAsync(false, ct);

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

    public async Task<WorkLocation> CreateAsync(
        string name, string colorHex, bool countsAsTravel, CancellationToken ct = default)
    {
        var all = await _store.GetAllAsync(includeArchived: true, ct);
        var active = all.Where(l => !l.IsArchived).ToList();
        LocationRules.ValidateNewOrRenamed(name, colorHex, active);

        var order = all.Count == 0 ? 0 : all.Max(l => l.DisplayOrder) + 1;
        var location = new WorkLocation
        {
            Name = LocationRules.NormalizeName(name),
            ColorHex = colorHex,
            CountsAsTravel = countsAsTravel,
            IsDefaultForUnmarked = false,
            IsArchived = false,
            DisplayOrder = order,
        };

        return await _store.AddAsync(location, ct);
    }

    public async Task UpdateAsync(
        int id, string name, string colorHex, bool countsAsTravel, CancellationToken ct = default)
    {
        var all = await _store.GetAllAsync(includeArchived: true, ct);
        var target = all.FirstOrDefault(l => l.Id == id)
            ?? throw new InvalidOperationException("That location no longer exists.");

        var active = all.Where(l => !l.IsArchived).ToList();
        LocationRules.ValidateNewOrRenamed(name, colorHex, active, editingId: id);

        target.Name = LocationRules.NormalizeName(name);
        target.ColorHex = colorHex;
        target.CountsAsTravel = countsAsTravel;
        await _store.UpdateAsync(target, ct);
    }

    public async Task SetDefaultAsync(int id, CancellationToken ct = default)
    {
        var all = await _store.GetAllAsync(includeArchived: true, ct);
        LocationRules.EnsureCanBeDefault(id, all);
        await _store.SetDefaultAsync(id, ct);
    }

    public async Task ArchiveAsync(int id, CancellationToken ct = default)
    {
        var all = await _store.GetAllAsync(includeArchived: true, ct);
        LocationRules.EnsureCanArchive(id, all);
        await _store.SetArchivedAsync(id, archived: true, ct);
    }

    public Task UnarchiveAsync(int id, CancellationToken ct = default)
        => _store.SetArchivedAsync(id, archived: false, ct);

    public async Task ReorderAsync(IReadOnlyList<int> orderedIds, CancellationToken ct = default)
    {
        var all = await _store.GetAllAsync(includeArchived: true, ct);
        LocationRules.ValidateReorder(orderedIds, all);
        await _store.ReorderAsync(orderedIds, ct);
    }
}