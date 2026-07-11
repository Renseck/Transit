using Microsoft.EntityFrameworkCore;
using Transit.Core.Entities;

namespace Transit.Infrastructure.Persistence;

public sealed class TransitDbContext : DbContext
{
    public TransitDbContext(DbContextOptions<TransitDbContext> options) : base(options) { }

    public DbSet<WorkLocation> WorkLocations => Set<WorkLocation>();
    public DbSet<AttendanceEntry> AttendanceEntries => Set<AttendanceEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkLocation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.ColorHex).IsRequired();
        });

        modelBuilder.Entity<AttendanceEntry>(e =>
        {
            e.HasKey(x => x.Date);
            e.HasOne<WorkLocation>()
                .WithMany()
                .HasForeignKey(x => x.WorkLocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}