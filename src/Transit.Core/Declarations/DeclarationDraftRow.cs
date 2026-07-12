namespace Transit.Core.Declarations;

// One review-grid row. Km is 0 for non-travel days and for travel days with no matching route.
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
    bool HasNoRoute);