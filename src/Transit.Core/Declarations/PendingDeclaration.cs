namespace Transit.Core.Declarations;

// A month that should be declared but has no run. Label is English ("August 2026") for the UI and
// toast; Query is the /declaration?month= value ("2026-08").
public sealed record PendingDeclaration(int Year, int Month, string Label, string Query);
