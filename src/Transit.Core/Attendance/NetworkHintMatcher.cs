using Transit.Core.Entities;

namespace Transit.Core.Attendance;

/// <summary>
/// Pure: which active location, if any, is hinted by one of the currently connected networks.
/// Exact match after trimming, ignoring case. When several locations match, the lowest
/// DisplayOrder wins. Archived locations and blank hints never match.
/// </summary>
public static class NetworkHintMatcher
{
    public static NetworkMatch? Match(IReadOnlyList<WorkLocation> locations, IReadOnlyList<string> networkNames)
    {
        var names = networkNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .ToList();

        var candidates = locations
            .Where(l => !l.IsArchived && !string.IsNullOrWhiteSpace(l.NetworkHint))
            .OrderBy(l => l.DisplayOrder);

        foreach (var location in candidates)
        {
            var hint = location.NetworkHint!.Trim();
            var network = names.FirstOrDefault(n => string.Equals(n, hint, StringComparison.OrdinalIgnoreCase));
            if (network is not null)
            {
                return new NetworkMatch(location, network);
            }
        }

        return null;
    }
}
