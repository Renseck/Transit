namespace Transit.Web.Endpoints;

// The location hinted by a connected network, and which network matched.
public sealed record ReminderSuggestion(int LocationId, string Name, string Network);
