using Transit.Core.Entities;

namespace Transit.Core.Abstractions;

public interface IAttendanceStore
{
    Task<IReadOnlyList<AttendanceEntry>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
    Task SetAsync(DateOnly date, int workLocationId, CancellationToken ct = default);
    Task ClearAsync(DateOnly date, CancellationToken ct = default);
}