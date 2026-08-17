namespace Transit.Core.Declarations;

// One month tile in the history year-grid. Month is 1..12. LatestRun/RunCount are null/0 when undeclared.
public sealed record MonthTileState(int Month, bool IsDeclared, DeclarationRunSummary? LatestRun, int RunCount);