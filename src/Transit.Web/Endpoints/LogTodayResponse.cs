namespace Transit.Web.Endpoints;

// Outcome is a QuickLogOutcome name; LocationName is the logged (or already-logged) location.
public sealed record LogTodayResponse(string Outcome, DateOnly? Date, string? LocationName);
