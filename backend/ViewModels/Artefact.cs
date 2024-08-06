using asec.Models;
using asec.Models.Archive;

namespace asec.ViewModels;

public record Artefact
{
    public string Id { get; set; }
    public string VersionId { get; set; }
    public string Name { get; set; }
    public string PhysicalMediaState { get; set; }
    public string ArchivationDate { get; set; }
    public string Archiver { get; set; }
    public string Note { get; set; }

    public async Task<Models.Digitalization.Artefact> ToDBEntity(AsecDBContext context)
    {
        var version = await context.Versions.FindAsync(Guid.Parse(VersionId));
        return new() {
            Id = Guid.Empty,
            Version = version,
            Name = Name,
            PhysicalMediaState = PhysicalMediaState,
            ArchivationDate = DateTime.Parse(ArchivationDate),
            Archiver = Archiver,
            Note = Note
        };
    }

    public static Artefact FromDBEntity(Models.Digitalization.Artefact artefact)
    {
        return new() {
            Id = artefact.Id.ToString(),
            VersionId = artefact.Version.Id.ToString(),
            Name = artefact.Name,
            PhysicalMediaState = artefact.PhysicalMediaState.ToString(),
            ArchivationDate = artefact.ArchivationDate.ToString(),
            Archiver = artefact.Archiver,
            Note = artefact.Note
        };
    }
}