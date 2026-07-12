using Transit.Core.Abstractions;
using Transit.Core.Declarations;
using Transit.Core.Entities;

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

    // The review grid: defaults only.
    public async Task<DeclarationDraft> BuildDraftAsync(int year, int month, CancellationToken ct = default)
    {
        var (entries, locations, routes) = await LoadAsync(year, month, ct);
        return DeclarationBuilder.Build(year, month, entries, locations, routes);
    }

    // Generation: defaults, then the grid's per-day route overrides applied so the result equals
    // what the user reviewed.
    public async Task<DeclarationDraft> BuildReviewedDraftAsync(
        int year, int month, IReadOnlyDictionary<DateOnly, int?> routeSelections, CancellationToken ct = default)
    {
        var (entries, locations, routes) = await LoadAsync(year, month, ct);
        var draft = DeclarationBuilder.Build(year, month, entries, locations, routes);
        return DeclarationBuilder.ApplySelections(draft, routeSelections, routes, locations);
    }

    private async Task<(IReadOnlyList<AttendanceEntry> entries, IReadOnlyList<WorkLocation> locations, IReadOnlyList<Route> routes)>
        LoadAsync(int year, int month, CancellationToken ct)
    {
        var from = new DateOnly(year, month, 1);
        var to = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        var entries = await _attendance.GetRangeAsync(from, to, ct);
        var locations = await _locations.GetAllAsync(includeArchived: true, ct);
        var routes = await _routes.GetAllAsync(ct);
        return (entries, locations, routes);
    }
}