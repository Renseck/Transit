using Transit.Core.Abstractions;

namespace Transit.Infrastructure.Persistence;

// Copies generated files into the user-chosen export folder. The folder is never created: a typo
// should surface as an error, not as a stray directory. Under IIS the app-pool identity needs
// Modify on it, which CheckAsync verifies with a probe file.
public sealed class FileSystemExportFolder : IExportFolder
{
    public async Task<string> WriteAsync(string folder, string fileName, byte[] content, CancellationToken ct = default)
    {
        if (!Directory.Exists(folder))
        {
            throw new DirectoryNotFoundException($"Export folder '{folder}' does not exist.");
        }

        var path = Path.Combine(folder, fileName);
        await File.WriteAllBytesAsync(path, content, ct);
        return path;
    }

    public async Task<string?> CheckAsync(string folder, CancellationToken ct = default)
    {
        if (!Path.IsPathFullyQualified(folder))
        {
            return @"Use a full path, e.g. C:\Users\you\OneDrive\Declaraties.";
        }

        if (!Directory.Exists(folder))
        {
            return $"Folder '{folder}' does not exist.";
        }

        var probe = Path.Combine(folder, $".transit-probe-{Guid.NewGuid():N}");
        try
        {
            await File.WriteAllBytesAsync(probe, [], ct);
            File.Delete(probe);
            return null;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return $"Transit cannot write to '{folder}': {ex.Message}";
        }
    }
}
