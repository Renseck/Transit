using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Transit.Infrastructure.Persistence;

namespace Transit.Tests.Support;

// An IDbContextFactory backed by one kept-open in-memory SQLite connection. Schema is created via
// EnsureCreated (the model matches migrations for these tables); Dispose closes the connection,
// which drops the in-memory database.
public sealed class TestDbContextFactory : IDbContextFactory<TransitDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TransitDbContext> _options;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<TransitDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new TransitDbContext(_options);
        db.Database.EnsureCreated();
    }

    public TransitDbContext CreateDbContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}