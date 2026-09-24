using Transit.Core.Declarations;

namespace Transit.Web.Endpoints;

/// <summary>
/// Loopback status used by the desktop reminder helper. PendingDeclaration is last month while it
/// has no run (the helper shows a "declare" toast), else null.
/// </summary>
public sealed record TodayStatus(
    DateOnly Date, bool ExplicitlyLogged, ReminderSuggestion? Suggestion, IReadOnlyList<ReminderOption> Options,
    PendingDeclaration? PendingDeclaration);
