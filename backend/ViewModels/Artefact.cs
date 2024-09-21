using asec.Models;
using asec.Models.Archive;
using asec.Models.Digitalization;

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
    public string Type { get; set; }
    public string PhysicalMediaType { get; set; }
    public string OriginalFilename { get; set; }
    public string DigitalizationToolId { get; set; }

    public async Task<Models.Digitalization.Artefact> ToDBEntity(AsecDBContext context)
    {
        Models.Archive.Version version = null;
        if (Guid.TryParse(VersionId, out var versionId))
            version = await context.Versions.FindAsync(versionId);
        Models.Digitalization.DigitalizationTool tool = null;
        if (Guid.TryParse(DigitalizationToolId, out var toolId))
            tool = await context.DigitalizationTools.FindAsync(toolId);

        var artefactType = ArtefactType.Unknown;
        if (Type != null)
            Enum.TryParse(Type, true, out artefactType);
        var mediaType = Models.Digitalization.PhysicalMediaType.Unknown;
        if (PhysicalMediaType != null)
            Enum.TryParse(PhysicalMediaType, true, out mediaType);
        
        return new() {
            Id = Guid.Empty,
            Version = version,
            Name = Name,
            PhysicalMediaState = PhysicalMediaState,
            ArchivationDate = string.IsNullOrEmpty(ArchivationDate) ? DateTime.Now : DateTime.Parse(ArchivationDate),
            Archiver = Archiver,
            Note = Note,
            Type = artefactType,
            PhysicalMediaType = mediaType,
            OriginalFilename = OriginalFilename,
            DigitalizationTool = tool
        };
    }

    public static Artefact FromDBEntity(Models.Digitalization.Artefact artefact)
    {
        return new() {
            Id = artefact.Id.ToString(),
            VersionId = artefact.Version.Id.ToString(),
            Name = artefact.Name,
            PhysicalMediaState = artefact.PhysicalMediaState,
            ArchivationDate = artefact.ArchivationDate.ToString(),
            Archiver = artefact.Archiver,
            Note = artefact.Note,
            Type = artefact.Type.ToString(),
            PhysicalMediaType = artefact.PhysicalMediaType.ToString(),
            OriginalFilename = artefact.OriginalFilename,
            DigitalizationToolId = artefact.DigitalizationTool.Id.ToString()
        };
    }
}