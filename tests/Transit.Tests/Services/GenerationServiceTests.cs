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

    [Fact]
    public async Task Generate_signs_only_the_pdf_with_todays_date()
    {
        var writer = new RecordingWriter();
        var converter = new RecordingConverter();
        var factory = new TestDbContextFactory();
        var root = Path.Combine(Path.GetTempPath(), "transit-generation-test", Guid.NewGuid().ToString("N"));
        try
        {
            var history = new DeclarationHistoryService(
                new SqliteDeclarationRunStore(factory),
                new FileSystemArtifactStore(Options.Create(new StorageOptions { DataRoot = root })));
            var service = new GenerationService(
                new StubProfileStore(Profile()), new DeclarationService(new EmptyAttendance(), new HomeOnly(), new NoRoutes()),
                writer, converter, history, new FixedTime(new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero)));

            var result = await service.GenerateAsync(2026, 8, new Dictionary<DateOnly, int?>());

            Assert.Equal([null, new DateOnly(2026, 9, 2)], writer.SignedOnCalls);
            Assert.Equal(SignedXlsx, converter.Input);
            Assert.Equal(UnsignedXlsx, result.XlsxBytes);
            Assert.Equal(Pdf, result.PdfBytes);
            Assert.Equal(UnsignedXlsx, await history.OpenArtifactAsync(result.RunId, ArtifactKind.Xlsx));
            Assert.Equal(Pdf, await history.OpenArtifactAsync(result.RunId, ArtifactKind.Pdf));
            Assert.Equal("08_2026_reiskosten_rve.pdf", result.PdfFileName);
        }
        finally
        {
            factory.Dispose();
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
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

        public Task<byte[]> ConvertToPdfAsync(byte[] xlsxBytes, CancellationToken ct = default)
        {
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
