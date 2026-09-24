using Transit.Core.Entities;

namespace Transit.Core.Abstractions;

public interface ISettingsStore
{
    // The saved settings, or the defaults when nothing has been saved yet.
    Task<AppSettings> GetAsync(CancellationToken ct = default);

    // Upserts the single settings row.
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
}
