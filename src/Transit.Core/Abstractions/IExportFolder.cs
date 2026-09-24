namespace Transit.Core.Abstractions;

// The user's export folder (outside DataRoot): a convenience copy of each signed PDF.
public interface IExportFolder
{
    // Writes content to {folder}\{fileName}, overwriting an older copy; returns the full path.
    // Throws when the folder is missing or not writable; never creates the folder.
    Task<string> WriteAsync(string folder, string fileName, byte[] content, CancellationToken ct = default);

    // Null when the folder is an existing, writable absolute path; otherwise the reason it is not.
    Task<string?> CheckAsync(string folder, CancellationToken ct = default);
}
