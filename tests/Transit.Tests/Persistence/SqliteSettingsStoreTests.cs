using Microsoft.EntityFrameworkCore;
using Transit.Core.Entities;
using Transit.Infrastructure.Persistence;
using Transit.Tests.Support;
using Xunit;

namespace Transit.Tests.Persistence;

public class SqliteSettingsStoreTests
{
    [Fact]
    public async Task Get_returns_defaults_before_anything_is_saved()
    {
        using var factory = new TestDbContextFactory();
        var settings = await new SqliteSettingsStore(factory).GetAsync();

        Assert.Equal(new AppSettings().EmailSubjectTemplate, settings.EmailSubjectTemplate);
        Assert.Null(settings.EmailRecipient);
    }

    [Fact]
    public async Task Save_round_trips_and_keeps_a_single_row()
    {
        using var factory = new TestDbContextFactory();
        var store = new SqliteSettingsStore(factory);

        await store.SaveAsync(new AppSettings { EmailRecipient = "a@example.com", EmailSubjectTemplate = "One" });
        await store.SaveAsync(new AppSettings { EmailRecipient = "b@example.com", EmailSubjectTemplate = "Two" });

        var settings = await store.GetAsync();
        Assert.Equal("b@example.com", settings.EmailRecipient);
        Assert.Equal("Two", settings.EmailSubjectTemplate);

        using var db = factory.CreateDbContext();
        Assert.Equal(1, await db.AppSettings.CountAsync());
    }
}
