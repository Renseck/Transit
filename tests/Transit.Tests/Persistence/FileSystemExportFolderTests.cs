using Transit.Infrastructure.Persistence;
using Xunit;

namespace Transit.Tests.Persistence;

public class FileSystemExportFolderTests : IDisposable
{
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), "transit-export-test", Guid.NewGuid().ToString("N"));

    public FileSystemExportFolderTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task Write_copies_the_file_and_overwrites_an_older_copy()
    {
        var export = new FileSystemExportFolder();

        await export.WriteAsync(_folder, "08_2026_reiskosten_rve.pdf", [1, 2]);
        var path = await export.WriteAsync(_folder, "08_2026_reiskosten_rve.pdf", [3]);

        Assert.Equal(Path.Combine(_folder, "08_2026_reiskosten_rve.pdf"), path);
        Assert.Equal([3], await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task Check_passes_for_a_writable_folder_and_leaves_no_probe_behind()
    {
        Assert.Null(await new FileSystemExportFolder().CheckAsync(_folder));
        Assert.Empty(Directory.GetFiles(_folder));
    }

    [Fact]
    public async Task Check_reports_a_missing_folder()
    {
        var reason = await new FileSystemExportFolder().CheckAsync(Path.Combine(_folder, "nope"));
        Assert.Contains("does not exist", reason);
    }

    [Fact]
    public async Task Check_rejects_a_relative_path()
    {
        var reason = await new FileSystemExportFolder().CheckAsync(@"exports\declarations");
        Assert.Contains("full path", reason);
    }

    [Fact]
    public async Task Write_to_a_missing_folder_throws_instead_of_creating_it()
        => await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => new FileSystemExportFolder().WriteAsync(Path.Combine(_folder, "nope"), "x.pdf", [1]));
}
