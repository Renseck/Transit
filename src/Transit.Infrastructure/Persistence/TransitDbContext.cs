using Microsoft.EntityFrameworkCore;
using Transit.Core.Entities;

namespace Transit.Infrastructure.Persistence;

public sealed class TransitDbContext : DbContext
{
    public TransitDbContext(DbContextOptions<TransitDbContext> options) : base(options) { }

    public DbSet<WorkLocation> WorkLocations => Set<WorkLocation>();
    public DbSet<AttendanceEntry> AttendanceEntries => Set<AttendanceEntry>();
    public DbSet<Route> Routes => Set<Route>();

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

        modelBuilder.Entity<Route>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.Ignore(x => x.TotalKm);   // computed, never a column
            e.HasOne<WorkLocation>()
                .WithMany()
                .HasForeignKey(x => x.FromLocationId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<WorkLocation>()
                .WithMany()
                .HasForeignKey(x => x.ToLocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}