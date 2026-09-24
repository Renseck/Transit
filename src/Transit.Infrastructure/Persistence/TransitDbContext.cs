using Microsoft.EntityFrameworkCore;
using Transit.Core.Entities;

namespace Transit.Infrastructure.Persistence;

public sealed class TransitDbContext : DbContext
{
    public TransitDbContext(DbContextOptions<TransitDbContext> options) : base(options) { }

    public DbSet<WorkLocation> WorkLocations => Set<WorkLocation>();
    public DbSet<AttendanceEntry> AttendanceEntries => Set<AttendanceEntry>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<TemplateProfile> TemplateProfiles => Set<TemplateProfile>();
    public DbSet<DeclarationRun> DeclarationRuns => Set<DeclarationRun>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();

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

        modelBuilder.Entity<TemplateProfile>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.TemplateFilePath).IsRequired();
            e.Property(x => x.SheetSelector).IsRequired();
            e.HasMany(x => x.ColumnMappings)
                .WithOne()
                .HasForeignKey(m => m.TemplateProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.CellMappings)
                .WithOne()
                .HasForeignKey(m => m.TemplateProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ColumnMapping>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Column).IsRequired();
            e.Property(x => x.Field).HasConversion<string>();   // store enum names, not ints
        });

        modelBuilder.Entity<CellMapping>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CellRef).IsRequired();
            e.Property(x => x.Source).HasConversion<string>();
        });

        modelBuilder.Entity<DeclarationRun>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TemplateName).IsRequired();
            e.Property(x => x.XlsxFilePath).IsRequired();
            e.HasMany(x => x.Rows)
                .WithOne()
                .HasForeignKey(r => r.DeclarationRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppSettings>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();   // singleton row, always Id = 1
            e.Property(x => x.EmailSubjectTemplate).IsRequired();
            e.Property(x => x.EmailBodyTemplate).IsRequired();
        });

        modelBuilder.Entity<DeclarationRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.WeekdayName).IsRequired();
            e.Property(x => x.LocationName).IsRequired();
            e.Property(x => x.ColorHex).IsRequired();
        });
    }
}