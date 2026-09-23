using Transit.Core.Entities;

namespace Transit.Core.Attendance;

/// <summary>
/// Pure: the locations offered as reminder-toast buttons — the suggested one first, then the other
/// active locations by DisplayOrder, capped so that, with the extra "Open" button, the toast stays
/// within Windows' five-button limit.
/// </summary>
public static class ReminderChoices
{
    public const int MaxLocations = 4;

    public static IReadOnlyList<WorkLocation> Build(IReadOnlyList<WorkLocation> locations, WorkLocation? suggested)
    {
        IEnumerable<WorkLocation> ordered = locations
            .Where(l => !l.IsArchived)
            .OrderBy(l => l.DisplayOrder);

        if (suggested is not null)
        {
            ordered = ordered.Where(l => l.Id != suggested.Id).Prepend(suggested);
        }

        return [.. ordered.Take(MaxLocations)];
    }
}
