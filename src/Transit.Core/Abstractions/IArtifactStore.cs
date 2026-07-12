namespace Transit.Core.Abstractions;

// Reads/writes generated declaration files on disk under generated/{yyyy-MM}/{runId}/.
// Hands back absolute paths; the DeclarationRun row stores those paths.
public interface IArtifactStore
{
    // Writes bytes to generated/{yyyy-MM}/{runId}/{fileName}; returns the absolute path.
    Task<string> SaveAsync(int year, int month, int runId, string fileName, byte[] content, CancellationToken ct = default);

    // Reads a stored artifact by its absolute path.
    Task<byte[]> ReadAsync(string path, CancellationToken ct = default);

    // Deletes the whole generated/{yyyy-MM}/{runId}/ folder.
    Task DeleteRunAsync(int year, int month, int runId, CancellationToken ct = default);
}