namespace Transit.Core.Declarations;

public sealed record DeclarationDraft(int Year, int Month, IReadOnlyList<DeclarationDraftRow> Rows);