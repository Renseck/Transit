using Transit.Core.Entities;
using Transit.Infrastructure.Persistence;
using Transit.Tests.Support;
using Xunit;

namespace Transit.Tests.Persistence;

public class SqliteDeclarationRunStoreTests
{
    private static DeclarationRun NewRun(int month, DateTime created, bool withPdf, int travelDays)
    {
        var rows = new List<DeclarationRow>();
        for (var i = 0; i < travelDays; i++)
        {
            rows.Add(new DeclarationRow
            {
                Date = new DateOnly(2026, month, i + 1), WeekdayName = "Wednesday",
                WorkLocationId = 2, LocationName = "Office", ColorHex = "#3B5BDB",
                CountsAsTravel = true, IsUnmarkedDefault = false, HasNoRoute = false,
                OutboundKm = 10m, ReturnKm = 10m, Kilometers = 20m,
            });
        }

        // One non-travel row that must NOT count toward TravelDayCount.
        rows.Add(new DeclarationRow
        {
            Date = new DateOnly(2026, month, 28), WeekdayName = "Monday",
            WorkLocationId = 1, LocationName = "Home", ColorHex = "#4C566A",
            CountsAsTravel = false, IsUnmarkedDefault = true, HasNoRoute = false,
        });

        return new DeclarationRun
        {
            PeriodYear = 2026, PeriodMonth = month, TemplateProfileId = 1, TemplateName = "Km",
            CreatedUtc = created, XlsxFilePath = "x.xlsx", PdfFilePath = withPdf ? "x.pdf" : null,
            Rows = rows,
        };
    }

    [Fact]
    public async Task Create_then_summaries_projects_counts_and_flags_newest_first()
    {
        using var factory = new TestDbContextFactory();
        var store = new SqliteDeclarationRunStore(factory);

        await store.CreateAsync(NewRun(6, new DateTime(2026, 6, 30), withPdf: true, travelDays: 3));
        await store.CreateAsync(NewRun(7, new DateTime(2026, 7, 31), withPdf: false, travelDays: 5));

        var summaries = await store.GetSummariesAsync();

        Assert.Equal(2, summaries.Count);
        Assert.Equal(7, summaries[0].PeriodMonth);      // newest first
        Assert.Equal(5, summaries[0].TravelDayCount);   // non-travel row excluded
        Assert.True(summaries[0].HasXlsx);
        Assert.False(summaries[0].HasPdf);              // xlsx-only run
        Assert.True(summaries[1].HasPdf);
        Assert.Equal(100m, summaries[0].TotalKm);       // 5 travel rows * 20 km
        Assert.Equal(60m, summaries[1].TotalKm);        // 3 travel rows * 20 km
    }

    [Fact]
    public async Task Get_includes_rows_and_delete_cascades()
    {
        using var factory = new TestDbContextFactory();
        var store = new SqliteDeclarationRunStore(factory);
        var created = await store.CreateAsync(NewRun(7, DateTime.UtcNow, withPdf: true, travelDays: 2));

        var loaded = await store.GetAsync(created.Id);
        Assert.NotNull(loaded);
        Assert.Equal(3, loaded!.Rows.Count);   // 2 travel + 1 non-travel

        await store.DeleteAsync(created.Id);
        Assert.Null(await store.GetAsync(created.Id));
    }

    [Fact]
    public async Task SetArtifactPaths_updates_the_row()
    {
        using var factory = new TestDbContextFactory();
        var store = new SqliteDeclarationRunStore(factory);
        var created = await store.CreateAsync(NewRun(7, DateTime.UtcNow, withPdf: false, travelDays: 1));

        await store.SetArtifactPathsAsync(created.Id, @"C:\x\declaration.xlsx", @"C:\x\declaration.pdf");

        var loaded = await store.GetAsync(created.Id);
        Assert.Equal(@"C:\x\declaration.xlsx", loaded!.XlsxFilePath);
        Assert.Equal(@"C:\x\declaration.pdf", loaded.PdfFilePath);
    }
}