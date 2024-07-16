namespace asec.ViewModels;

public record Artefact(
    string Id,
    string VersionId,
    string Name,
    string PhysicalMediaState,
    string ArchivationDate,
    string Archiver,
    string Note
);