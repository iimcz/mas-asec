using asec.Models.Archive;
using Microsoft.EntityFrameworkCore;

namespace asec.Models;

public class AsecDBContext : DbContext
{
    public DbSet<Work> Works { get; set; }

    public DbSet<Classification> Classifications { get; set; }
    public DbSet<TimeClassification> TimeClassifications { get; set; }
    public DbSet<LocationClassification> LocationClassifications { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<Status> Statuses { get; set; }
    public DbSet<Language> Languages { get; set; }

    public AsecDBContext(DbContextOptions<AsecDBContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Work>().HasMany<Status>().WithMany();
        modelBuilder.Entity<Work>().HasMany<Classification>().WithMany();
        modelBuilder.Entity<Work>().HasMany<TimeClassification>().WithMany();
        modelBuilder.Entity<Work>().HasMany<LocationClassification>().WithMany();
        modelBuilder.Entity<Work>().HasMany<Genre>().WithMany();

        // TODO: seed DB
    }
}