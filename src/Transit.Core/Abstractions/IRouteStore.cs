using Transit.Core.Entities;

namespace Transit.Core.Abstractions;

public interface IRouteStore
{
    Task<IReadOnlyList<Route>> GetAllAsync(CancellationToken ct = default);
    Task<Route> AddAsync(Route route, CancellationToken ct = default);
    Task UpdateAsync(Route route, CancellationToken ct = default);
    Task DeleteAsync(int routeId, CancellationToken ct = default);

    // Atomic: within the target route's ToLocationId group, demote siblings and promote it.
    Task SetDefaultAsync(int routeId, CancellationToken ct = default);
}