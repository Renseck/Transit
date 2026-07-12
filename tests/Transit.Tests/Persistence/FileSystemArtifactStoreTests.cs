using Microsoft.Extensions.Options;
using Transit.Infrastructure.Configuration;
using Transit.Infrastructure.Persistence;
using Xunit;

namespace Transit.Tests.Persistence;

public class FileSystemArtifactStoreTests
{
    private static (FileSystemArtifactStore store, string root) NewStore()
    {
        var root = Path.Combine(Path.GetTempPath(), "transit-artifacts-test", Guid.NewGuid().ToString("N"));
        var store = new FileSystemArtifactStore(Options.Create(new StorageOptions { DataRoot = root }));
        return (store, root);
    }

    [Fact]
    public async Task Save_then_read_round_trips_the_bytes()
    {
        var (store, root) = NewStore();
        try
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            var path = await store.SaveAsync(2026, 7, 42, "declaration.xlsx", bytes);

            Assert.True(File.Exists(path));
            Assert.Contains(Path.Combine("generated", "2026-07", "42"), path);
            Assert.Equal(bytes, await store.ReadAsync(path));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task DeleteRun_removes_the_run_folder()
    {
        var (store, root) = NewStore();
        try
        {
            var path = await store.SaveAsync(2026, 7, 42, "declaration.xlsx", [1]);
            await store.DeleteRunAsync(2026, 7, 42);

            Assert.False(File.Exists(path));
            Assert.False(Directory.Exists(Path.GetDirectoryName(path)!));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}