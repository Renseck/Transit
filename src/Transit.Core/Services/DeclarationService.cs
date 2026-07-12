using Transit.Core.Abstractions;
using Transit.Core.Declarations;

namespace Transit.Core.Services;

/// <summary>
/// Loads a month's attendance, all locations (incl. archived), and all routes, then delegates
/// to the pure <see cref="DeclarationBuilder"/>. No business logic of its own beyond loading.
/// </summary>
public sealed class DeclarationService
{
    private readonly IAttendanceStore _attendance;
    private readonly ILocationStore _locations;
    private readonly IRouteStore _routes;

    public DeclarationService(IAttendanceStore attendance, ILocationStore locations, IRouteStore routes)
    {
        _attendance = attendance;
        _locations = locations;
        _routes = routes;
    }

    public async Task<DeclarationDraft> BuildDraftAsync(int year, int month, CancellationToken ct = default)
    {
        var from = new DateOnly(year, month, 1);
        var to = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        var entries = await _attendance.GetRangeAsync(from, to, ct);
        var locations = await _locations.GetAllAsync(includeArchived: true, ct);
        var routes = await _routes.GetAllAsync(ct);

        return DeclarationBuilder.Build(year, month, entries, locations, routes);
    }
}