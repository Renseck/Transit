using Microsoft.Extensions.Options;
using Transit.Core.Abstractions;
using Transit.Infrastructure.Configuration;

namespace Transit.Infrastructure.Persistence;

/// <summary>
/// Stores generated declaration files under {DataRoot}\generated\{yyyy-MM}\{runId}\. Append-only:
/// a new run gets a new folder; existing folders are never overwritten.
/// </summary>
public sealed class FileSystemArtifactStore : IArtifactStore
{
    private readonly StorageOptions _storage;

    public FileSystemArtifactStore(IOptions<StorageOptions> storage) => _storage = storage.Value;

    public async Task<string> SaveAsync(
        int year, int month, int runId, string fileName, byte[] content, CancellationToken ct = default)
    {
        var dir = RunDirectory(year, month, runId);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, fileName);
        await File.WriteAllBytesAsync(path, content, ct);
        return path;
    }

    public async Task<byte[]> ReadAsync(string path, CancellationToken ct = default)
        => await File.ReadAllBytesAsync(path, ct);

    public Task DeleteRunAsync(int year, int month, int runId, CancellationToken ct = default)
    {
        var dir = RunDirectory(year, month, runId);
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }

        return Task.CompletedTask;
    }

    private string RunDirectory(int year, int month, int runId)
        => Path.Combine(_storage.DataRoot, "generated", $"{year:D4}-{month:D2}", runId.ToString());
}