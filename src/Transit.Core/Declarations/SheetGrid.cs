namespace Transit.Core.Declarations;

// A rendered sheet region: column letters across the top, then rows of display strings
// (one inner list per sheet row, left to right).
public sealed record SheetGrid(
    IReadOnlyList<string> ColumnLetters,
    IReadOnlyList<IReadOnlyList<string>> Rows);