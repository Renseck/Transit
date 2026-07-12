using Transit.Core.Abstractions;
using Transit.Core.Entities;
using Transit.Core.Routes;

namespace Transit.Core.Services;

public sealed class RouteService
{
    private readonly IRouteStore _routes;
    private readonly ILocationStore _locations;

    public RouteService(IRouteStore routes, ILocationStore locations)
    {
        _routes = routes;
        _locations = locations;
    }

    public Task<IReadOnlyList<Route>> GetAllAsync(CancellationToken ct = default) => _routes.GetAllAsync(ct);

    public async Task<Route> CreateAsync(
        string name, int fromId, int toId, decimal outboundKm, decimal returnKm, bool makeDefault,
        CancellationToken ct = default)
    {
        var routes = await _routes.GetAllAsync(ct);
        var active = await _locations.GetAllAsync(includeArchived: false, ct);
        RouteRules.ValidateNewOrRenamed(name, fromId, toId, outboundKm, returnKm, routes, active);

        // The first route to a destination is automatically that destination's default.
        var isFirstToDestination = routes.All(r => r.ToLocationId != toId);
        var route = new Route
        {
            Name = RouteRules.NormalizeName(name),
            FromLocationId = fromId,
            ToLocationId = toId,
            OutboundKm = outboundKm,
            ReturnKm = returnKm,
            IsDefault = isFirstToDestination,
        };

        var created = await _routes.AddAsync(route, ct);

        if (makeDefault && !isFirstToDestination)
        {
            await _routes.SetDefaultAsync(created.Id, ct);
        }

        return created;
    }

    // Endpoints are fixed after creation (see spec §6); this edits name + km only.
    public async Task UpdateAsync(
        int id, string name, decimal outboundKm, decimal returnKm, CancellationToken ct = default)
    {
        var routes = await _routes.GetAllAsync(ct);
        var target = routes.FirstOrDefault(r => r.Id == id)
            ?? throw new InvalidOperationException("That route no longer exists.");

        var active = await _locations.GetAllAsync(includeArchived: false, ct);
        RouteRules.ValidateNewOrRenamed(
            name, target.FromLocationId, target.ToLocationId, outboundKm, returnKm, routes, active, editingId: id);

        target.Name = RouteRules.NormalizeName(name);
        target.OutboundKm = outboundKm;
        target.ReturnKm = returnKm;
        await _routes.UpdateAsync(target, ct);
    }

    public Task SetDefaultAsync(int id, CancellationToken ct = default) => _routes.SetDefaultAsync(id, ct);

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var routes = await _routes.GetAllAsync(ct);
        var target = routes.FirstOrDefault(r => r.Id == id);
        if (target is null)
        {
            return;
        }

        await _routes.DeleteAsync(id, ct);

        // If the deleted route was its destination's default, promote a sibling.
        if (target.IsDefault)
        {
            var promotion = RouteRules.PickPromotionAfterDelete(id, routes);
            if (promotion is not null)
            {
                await _routes.SetDefaultAsync(promotion.Id, ct);
            }
        }
    }
}