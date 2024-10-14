using asec.Extensions;
using asec.Models.Archive;
using asec.Models.Digitalization;
using asec.Models.Emulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace asec.Models;

public class AsecDBContext : DbContext
{
    public DbSet<Work> Works { get; set; }
    public DbSet<Archive.Version> Versions { get; set; }
    public DbSet<Artefact> Artefacts { get; set; }

    public DbSet<Classification> Classifications { get; set; }
    public DbSet<TimeClassification> TimeClassifications { get; set; }
    public DbSet<LocationClassification> LocationClassifications { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<Status> Statuses { get; set; }
    public DbSet<Archive.System> Systems { get; set; }
    public DbSet<Language> Languages { get; set; }

    public DbSet<DigitalizationTool> DigitalizationTools { get; set; }

    public DbSet<Platform> Platforms { get; set; }
    public DbSet<Emulator> Emulators { get; set; }
    public DbSet<EmulationEnvironment> Environments { get; set; }
    public DbSet<Converter> Converters { get; set; }
    public DbSet<GamePackage> GamePackages { get; set; }

    public DbSet<Paratext> Paratexts { get; set; }

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

        modelBuilder.Entity<GamePackage>().HasMany(p => p.IncludedArtefacts).WithMany();

        modelBuilder.Entity<Emulator>().HasMany(e => e.Platforms).WithMany();

        modelBuilder.Entity<EmulationEnvironment>().HasMany(e => e.Converters).WithMany();


        var platformListComparer = new ValueComparer<IList<PhysicalMediaType>>(
            (a, b) => a != null && b != null && a.SequenceEqual(b),
            (v) => v.Sum(p => 1 << ((int)p % 32))
        );
        modelBuilder.Entity<Platform>().Property(p => p.MediaTypes).HasConversion(
            v => string.Join(',', v.Select(e => e.ToString())),
            v => v.Split(',', StringSplitOptions.None).Select(e => Enum.Parse<PhysicalMediaType>(e)).ToList(),
            platformListComparer
        );

        var artefactTypeListComparer = new ValueComparer<IList<ArtefactType>>(
            (a, b) => a != null && b != null && a.SequenceEqual(b),
            (v) => v.Sum(t => 1 << ((int)t % 32))
        );
        modelBuilder.Entity<Converter>().Property(c => c.SupportedArtefactTypes).HasConversion(
            v => string.Join(',', v.Select(e => e.ToString())),
            v => v.Split(',', StringSplitOptions.None).Select(e => Enum.Parse<ArtefactType>(e)).ToList(),
            artefactTypeListComparer
        );

        // TODO: seed DB
    }
}