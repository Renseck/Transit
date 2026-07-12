using Microsoft.Extensions.Options;
using Transit.Core.Declarations;
using Transit.Core.Entities;
using Transit.Core.Services;
using Transit.Infrastructure.Configuration;
using Transit.Infrastructure.Persistence;
using Transit.Tests.Support;
using Xunit;

namespace Transit.Tests.Services;

public class DeclarationHistoryServiceTests
{
    // One travel row (Office) + one non-travel row (Home) → TravelDayCount should be 1.
    private static DeclarationDraft Draft() => new(2026, 7, new List<DeclarationDraftRow>
    {
        new(new DateOnly(2026, 7, 1), "Wednesday", 2, "Office", "#3B5BDB", true, false,
            10, "Home → Office", 11m, 10m, 21m, false, "Home", "1234 AB", "Office", "5678 CD"),
        new(new DateOnly(2026, 7, 2), "Thursday", 1, "Home", "#4C566A", false, true,
            null, null, 0m, 0m, 0m, false),
    });

    private static TemplateProfile Profile() => new()
    {
        Id = 1, Name = "Km", TemplateFilePath = "x", SheetSelector = "Blad1",
        DataStartRow = 8, DataRowCount = 15, IsActive = true, CreatedUtc = DateTime.UtcNow,
    };

    private static (DeclarationHistoryService svc, TestDbContextFactory factory, string root) NewService()
    {
        var factory = new TestDbContextFactory();
        var root = Path.Combine(Path.GetTempPath(), "transit-history-test", Guid.NewGuid().ToString("N"));
        var runs = new SqliteDeclarationRunStore(factory);
        var artifacts = new FileSystemArtifactStore(Options.Create(new StorageOptions { DataRoot = root }));
        return (new DeclarationHistoryService(runs, artifacts), factory, root);
    }

    [Fact]
    public async Task Save_persists_run_rows_and_both_artifacts()
    {
        var (svc, factory, root) = NewService();
        try
        {
            var run = await svc.SaveAsync(Draft(), Profile(), [1, 2, 3], [4, 5]);

            Assert.True(File.Exists(run.XlsxFilePath));
            Assert.NotNull(run.PdfFilePath);
            Assert.True(File.Exists(run.PdfFilePath!));
            Assert.Equal([1, 2, 3], await svc.OpenArtifactAsync(run.Id, ArtifactKind.Xlsx));
            Assert.Equal([4, 5], await svc.OpenArtifactAsync(run.Id, ArtifactKind.Pdf));

            var summaries = await svc.GetSummariesAsync();
            Assert.Single(summaries);
            Assert.Equal("Km", summaries[0].TemplateName);
            Assert.Equal(1, summaries[0].TravelDayCount);   // only the Office row
            Assert.True(summaries[0].HasPdf);
        }
        finally
        {
            factory.Dispose();
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Save_without_pdf_is_xlsx_only()
    {
        var (svc, factory, root) = NewService();
        try
        {
            var run = await svc.SaveAsync(Draft(), Profile(), [1], pdf: null);

            Assert.Null(run.PdfFilePath);
            var summaries = await svc.GetSummariesAsync();
            Assert.False(summaries[0].HasPdf);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => svc.OpenArtifactAsync(run.Id, ArtifactKind.Pdf));
        }
        finally
        {
            factory.Dispose();
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Delete_removes_the_run_and_its_folder()
    {
        var (svc, factory, root) = NewService();
        try
        {
            var run = await svc.SaveAsync(Draft(), Profile(), [1], [2]);
            var folder = Path.GetDirectoryName(run.XlsxFilePath)!;
            Assert.True(Directory.Exists(folder));

            await svc.DeleteAsync(run.Id);

            Assert.Empty(await svc.GetSummariesAsync());
            Assert.False(Directory.Exists(folder));
        }
        finally
        {
            factory.Dispose();
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}