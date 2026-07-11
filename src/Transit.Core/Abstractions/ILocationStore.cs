using Transit.Core.Entities;

namespace Transit.Core.Abstractions;

public interface ILocationStore
{
    Task<IReadOnlyList<WorkLocation>> GetAllAsync(CancellationToken ct = default);
}