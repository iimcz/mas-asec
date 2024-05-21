using asec.Models.Archive;
using Microsoft.EntityFrameworkCore;

namespace asec.Models;

public class AsecDBContext : DbContext
{
    public DbSet<Work> Works { get; set; }
    public DbSet<Archive.Version> Versions { get; set; }

    public DbSet<Classification> Classifications { get; set; }
    public DbSet<TimeClassification> TimeClassifications { get; set; }
    public DbSet<LocationClassification> LocationClassifications { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<Status> Statuses { get; set; }
    public DbSet<Archive.System> Systems { get; set; }
    public DbSet<Language> Languages { get; set; }

    public AsecDBContext(DbContextOptions<AsecDBContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Work>().HasMany(w => w.Status).WithMany();
        modelBuilder.Entity<Work>().HasMany(w => w.Classification).WithMany();
        modelBuilder.Entity<Work>().HasMany(w => w.TimeClassification).WithMany();
        modelBuilder.Entity<Work>().HasMany(w => w.LocationClassification).WithMany();
        modelBuilder.Entity<Work>().HasMany(w => w.Genre).WithMany();

        modelBuilder.Entity<Archive.Version>().HasMany(v => v.Status).WithMany();
        modelBuilder.Entity<Archive.Version>().HasMany(v => v.System).WithMany();

        // TODO: seed DB
    }
}