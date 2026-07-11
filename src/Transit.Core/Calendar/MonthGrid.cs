namespace Transit.Core.Calendar;

public sealed record MonthGrid(int Year, int Month, IReadOnlyList<IReadOnlyList<CalendarDay>> Weeks);