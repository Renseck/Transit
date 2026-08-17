using Microsoft.Extensions.Options;
using Transit.Core.Entities;
using Transit.Infrastructure.Configuration;
using Transit.Infrastructure.Persistence;
using Transit.Tests.Support;
using Xunit;

namespace Transit.Tests.Templates;

public class SqliteTemplateProfileStoreRenameTests
{
    private static SqliteTemplateProfileStore NewStore(TestDbContextFactory factory)
        => new(factory, Options.Create(new StorageOptions { DataRoot = Path.GetTempPath() }));

    private static async Task<int> SeedProfileAsync(TestDbContextFactory factory, string name)
    {
        await using var db = factory.CreateDbContext();
        var profile = new TemplateProfile
        {
            Name = name,
            TemplateFilePath = "x.xlsx",
            SheetSelector = "Sheet1",
            DataStartRow = 1,
            DataRowCount = 1,
            CreatedUtc = DateTime.UtcNow,
        };
        db.TemplateProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile.Id;
    }

    [Fact]
    public async Task RenameAsync_updates_the_name()
    {
        using var factory = new TestDbContextFactory();
        var store = NewStore(factory);
        var id = await SeedProfileAsync(factory, "old.xlsx");

        await store.RenameAsync(id, "  Truston 2026  ");

        var all = await store.GetAllAsync();
        Assert.Equal("Truston 2026", all.Single(p => p.Id == id).Name);
    }

    [Fact]
    public async Task RenameAsync_rejects_blank_name()
    {
        using var factory = new TestDbContextFactory();
        var store = NewStore(factory);
        var id = await SeedProfileAsync(factory, "keep.xlsx");

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.RenameAsync(id, "   "));
    }
}