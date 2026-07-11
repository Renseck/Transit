namespace Transit.Core.Calendar;

public readonly record struct CalendarDay(DateOnly Date, bool InCurrentMonth);