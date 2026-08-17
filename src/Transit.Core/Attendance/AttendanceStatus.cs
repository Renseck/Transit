using Transit.Core.Entities;

namespace Transit.Core.Attendance;

/// <summary>
/// Pure decision over stored attendance: is a given date explicitly logged?
/// The silent unmarked/Home default never counts — it is materialised at declaration
/// time and is never persisted, so only a real stored entry means "logged".
/// </summary>
public static class AttendanceStatus
{
    public static bool IsExplicitlyLogged(IReadOnlyList<AttendanceEntry> entries, DateOnly date)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i].Date == date) return true;
        }
        return false;
    }
}