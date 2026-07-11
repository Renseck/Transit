namespace Transit.Core.Entities;

public class AttendanceEntry
{
    public DateOnly Date { get; set; }   // primary key: one location per date
    public int WorkLocationId { get; set; }
}