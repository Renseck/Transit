using Transit.Core.Entities;

namespace Transit.Core.Abstractions;

public interface ILocationStore
{
    Task<IReadOnlyList<WorkLocation>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default);
    Task<WorkLocation> AddAsync(WorkLocation location, CancellationToken ct = default);
    Task UpdateAsync(WorkLocation location, CancellationToken ct = default);

    // Atomic: demote the current default and promote this one, in a single transaction.
    Task SetDefaultAsync(int locationId, CancellationToken ct = default);

    Task SetArchivedAsync(int locationId, bool archived, CancellationToken ct = default);

    // Persist the given ids as DisplayOrder 0..n-1.
    Task ReorderAsync(IReadOnlyList<int> orderedIds, CancellationToken ct = default);
}