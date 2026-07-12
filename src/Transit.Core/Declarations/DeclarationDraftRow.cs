namespace Transit.Core.Declarations;

// One review-grid row. Km is 0 for non-travel days and for travel days with no matching route.
// From/To name + postcode are populated only when a route resolves (travel days); otherwise null.
public sealed record DeclarationDraftRow(
    DateOnly Date,
    string WeekdayName,
    int WorkLocationId,
    string LocationName,
    string ColorHex,
    bool CountsAsTravel,
    bool IsUnmarkedDefault,
    int? SelectedRouteId,
    string? RouteName,
    decimal OutboundKm,
    decimal ReturnKm,
    decimal Kilometers,
    bool HasNoRoute,
    string? FromLocationName = null,
    string? FromPostcode = null,
    string? ToLocationName = null,
    string? ToPostcode = null);