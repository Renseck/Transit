namespace Transit.Web.Endpoints;

/// <summary>Loopback status used by the desktop reminder helper.</summary>
public sealed record TodayStatus(
    DateOnly Date, bool ExplicitlyLogged, ReminderSuggestion? Suggestion, IReadOnlyList<ReminderOption> Options);
