namespace Transit.Core.Declarations;

// Travel distance + travel-day count for a single month's view. TotalKm is never a template total.
public sealed record TravelKmSummary(decimal TotalKm, int TravelDayCount);