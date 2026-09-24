using Microsoft.Extensions.Options;
using Transit.Core.Abstractions;
using Transit.Core.Declarations;
using Transit.Core.Entities;
using Transit.Core.Services;
using Transit.Infrastructure.Configuration;
using Transit.Infrastructure.Persistence;
using Transit.Tests.Support;
using Xunit;

namespace Transit.Tests.Services;

// Only the PDF is signed: the stored xlsx is the unsigned fill, the converter gets the signed fill.
public class GenerationServiceTests
{
    private static readonly byte[] UnsignedXlsx = [1];
    private static readonly byte[] SignedXlsx = [2];
    private static readonly byte[] Pdf = [9];

    private static TemplateProfile Profile() => new()
    {
        Id = 1, Name = "Km", TemplateFilePath = "x", SheetSelector = "Blad1",
        DataStartRow = 8, DataRowCount = 15, IsActive = true, CreatedUtc = DateTime.UtcNow,
        CellMappings =
        [
            new CellMapping { CellRef = "C5", Source = TemplateCellSource.EmployeeName, Value = "Rens van Eck" },
            new CellMapping { CellRef = "B50", Source = TemplateCellSource.SignatureName },
        ],
    };

    // Builds the service over fakes + a real in-memory history; dispose cleans the DB and artifact root.
    private sealed class Harness : IDisposable
    {
        private readonly TestDbContextFactory _factory = new();
        private readonly string _root = Path.Combine(Path.GetTempPath(), "transit-generation-test", Guid.NewGuid().ToString("N"));

        public RecordingWriter Writer { get; } = new();
        public RecordingConverter Converter { get; } = new();
        public RecordingExportFolder Export { get; } = new();
        public AppSettings Settings { get; } = new();
        public DeclarationHistoryService History { get; }
        public GenerationService Service { get; }

        public Harness()
        {
            History = new DeclarationHistoryService(
                new SqliteDeclarationRunStore(_factory),
                new FileSystemArtifactStore(Options.Create(new StorageOptions { DataRoot = _root })));
            Service = new GenerationService(
                new StubProfileStore(Profile()), new DeclarationService(new EmptyAttendance(), new HomeOnly(), new NoRoutes()),
                Writer, Converter, History, new FixedTime(new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero)),
                new StubSettingsStore(Settings), Export);
        }

        public Task<GeneratedDeclaration> GenerateAugustAsync()
            => Service.GenerateAsync(2026, 8, new Dictionary<DateOnly, int?>());

        public void Dispose()
        {
            _factory.Dispose();
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task Generate_signs_only_the_pdf_with_todays_date()
    {
        using var h = new Harness();
        var writer = h.Writer;
        var converter = h.Converter;
        var history = h.History;
        {
            var result = await h.GenerateAugustAsync();

            Assert.Equal([null, new DateOnly(2026, 9, 2)], writer.SignedOnCalls);
            Assert.Equal(SignedXlsx, converter.Input);
            Assert.Equal(UnsignedXlsx, result.XlsxBytes);
            Assert.Equal(Pdf, result.PdfBytes);
            Assert.Equal(UnsignedXlsx, await history.OpenArtifactAsync(result.RunId, ArtifactKind.Xlsx));
            Assert.Equal(Pdf, await history.OpenArtifactAsync(result.RunId, ArtifactKind.Pdf));
            Assert.Equal("08_2026_reiskosten_rve.pdf", result.PdfFileName);
        }
    }

    [Fact]
    public async Task Generate_without_an_export_folder_exports_nothing()
    {
        using var h = new Harness();

        var result = await h.GenerateAugustAsync();

        Assert.Empty(h.Export.Writes);
        Assert.Null(result.ExportedPath);
        Assert.Null(result.ExportError);
    }

    [Fact]
    public async Task Generate_copies_the_signed_pdf_to_the_export_folder_under_its_download_name()
    {
        using var h = new Harness();
        h.Settings.ExportFolder = @"C:\Exports";

        var result = await h.GenerateAugustAsync();

        var write = Assert.Single(h.Export.Writes);
        Assert.Equal((@"C:\Exports", "08_2026_reiskosten_rve.pdf"), (write.Folder, write.FileName));
        Assert.Equal(Pdf, write.Content);
        Assert.Equal(@"C:\Exports\08_2026_reiskosten_rve.pdf", result.ExportedPath);
    }

    [Fact]
    public async Task Export_failure_keeps_the_run_and_reports_the_reason()
    {
        using var h = new Harness();
        h.Settings.ExportFolder = @"C:\Exports";
        h.Export.Failure = new UnauthorizedAccessException("Access denied.");

        var result = await h.GenerateAugustAsync();

        Assert.Null(result.ExportedPath);
        Assert.Equal("Access denied.", result.ExportError);
        Assert.Equal(Pdf, await h.History.OpenArtifactAsync(result.RunId, ArtifactKind.Pdf));   // run still saved
    }

    [Fact]
    public async Task Generate_without_a_pdf_exports_nothing()
    {
        using var h = new Harness();
        h.Settings.ExportFolder = @"C:\Exports";
        h.Converter.Failure = new InvalidOperationException("LibreOffice missing");

        var result = await h.GenerateAugustAsync();

        Assert.Null(result.PdfBytes);
        Assert.Empty(h.Export.Writes);
    }

    private sealed class RecordingExportFolder : IExportFolder
    {
        public List<(string Folder, string FileName, byte[] Content)> Writes { get; } = [];
        public Exception? Failure { get; set; }

        public Task<string> WriteAsync(string folder, string fileName, byte[] content, CancellationToken ct = default)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            Writes.Add((folder, fileName, content));
            return Task.FromResult(Path.Combine(folder, fileName));
        }

        public Task<string?> CheckAsync(string folder, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubSettingsStore(AppSettings settings) : ISettingsStore
    {
        public Task<AppSettings> GetAsync(CancellationToken ct = default) => Task.FromResult(settings);
        public Task SaveAsync(AppSettings s, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class RecordingWriter : IExcelWriter
    {
        public List<DateOnly?> SignedOnCalls { get; } = [];

        public Task<byte[]> FillAsync(
            TemplateProfile profile, Stream templateBytes, DeclarationDraft draft,
            DateOnly? signedOn = null, CancellationToken ct = default)
        {
            Assert.True(templateBytes.CanRead);   // each fill gets a readable template
            SignedOnCalls.Add(signedOn);
            return Task.FromResult(signedOn is null ? UnsignedXlsx : SignedXlsx);
        }
    }

    private sealed class RecordingConverter : IPdfConverter
    {
        public byte[]? Input { get; private set; }
        public Exception? Failure { get; set; }

        public Task<byte[]> ConvertToPdfAsync(byte[] xlsxBytes, CancellationToken ct = default)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            Input = xlsxBytes;
            return Task.FromResult(Pdf);
        }
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class StubProfileStore(TemplateProfile profile) : ITemplateProfileStore
    {
        public Task<TemplateProfile?> GetActiveAsync(CancellationToken ct = default) => Task.FromResult<TemplateProfile?>(profile);
        public Task<Stream> OpenTemplateAsync(int id, CancellationToken ct = default) => Task.FromResult<Stream>(new MemoryStream([7, 7, 7]));
        public Task<IReadOnlyList<TemplateProfile>> GetAllAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<TemplateProfile> CreateAsync(TemplateProfile p, Stream s, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateMappingsAsync(int id, IReadOnlyList<ColumnMapping> columns, IReadOnlyList<CellMapping> cells,
            string sheetSelector, int dataStartRow, int dataRowCount, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SetActiveAsync(int id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task RenameAsync(int id, string name, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(int id, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class EmptyAttendance : IAttendanceStore
    {
        public Task<IReadOnlyList<AttendanceEntry>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AttendanceEntry>>([]);
        public Task SetAsync(DateOnly date, int workLocationId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ClearAsync(DateOnly date, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class HomeOnly : ILocationStore
    {
        public Task<IReadOnlyList<WorkLocation>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<WorkLocation>>(
                [new WorkLocation { Id = 1, Name = "Home", ColorHex = "#4C566A", IsDefaultForUnmarked = true }]);
        public Task<WorkLocation> AddAsync(WorkLocation location, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(WorkLocation location, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SetDefaultAsync(int locationId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SetArchivedAsync(int locationId, bool archived, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ReorderAsync(IReadOnlyList<int> orderedIds, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class NoRoutes : IRouteStore
    {
        public Task<IReadOnlyList<Route>> GetAllAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Route>>([]);
        public Task<Route> AddAsync(Route route, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(Route route, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(int routeId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SetDefaultAsync(int routeId, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
