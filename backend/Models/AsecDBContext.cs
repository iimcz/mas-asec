using asec.Models.Archive;
using asec.Models.Digitalization;
using asec.Models.Emulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace asec.Models;

public class AsecDBContext : DbContext
{
    public DbSet<Work> Works { get; set; }
    public DbSet<Archive.WorkVersion> WorkVersions { get; set; }
    public DbSet<PhysicalObject> PhysicalObjects { get; set; }
    public DbSet<DigitalObject> DigitalObjects { get; set; }

    public DbSet<DigitalizationTool> DigitalizationTools { get; set; }

    public DbSet<Platform> Platforms { get; set; }
    public DbSet<Emulator> Emulators { get; set; }
    public DbSet<EmulationEnvironment> Environments { get; set; }
    public DbSet<Converter> Converters { get; set; }

    public DbSet<Paratext> Paratexts { get; set; }

    public AsecDBContext(DbContextOptions<AsecDBContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WorkVersion>().HasMany(w => w.DigitalObjects).WithMany(o => o.Versions);
        modelBuilder.Entity<WorkVersion>().HasMany(w => w.PhysicalObjects).WithMany(o => o.Versions);

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
