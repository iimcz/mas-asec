using asec.Models;
using asec.Models.Digitalization;

namespace asec.ViewModels;

public record Artefact
{
    public string Id { get; set; }
    public string FileName { get; set; }
    public string InternalNote { get; set; }
    public string WebsiteUrl { get; set; }
    public string DigitalObjectType { get; set; }
    public string Format { get; set; }
    public uint FileSize { get; set; }
    public string Quality { get; set; }
    public string FedoraUrl { get; set; }

    public string Type { get; set;}
    public string PhysicalMediaState { get; set; }
    public string PhysicalMediaType { get; set; }

    public List<string> ParatextIds { get; set; }
    public List<string> VersionIds { get; set; }

    public async Task<Models.Digitalization.Artefact> ToDBEntity(AsecDBContext context)
    {
        var artefactType = ArtefactType.Unknown;
        if (Type != null)
            Enum.TryParse(Type, true, out artefactType);
        var mediaType = Models.Digitalization.PhysicalMediaType.Unknown;
        if (PhysicalMediaType != null)
            Enum.TryParse(PhysicalMediaType, true, out mediaType);
        var mediaState = Models.Digitalization.PhysicalMediaState.Good;
        if (PhysicalMediaState != null)
            Enum.TryParse(PhysicalMediaState, true, out mediaState);
        
        return new() {
            Id = Guid.Empty,
            ArchivationDate = DateTime.Now,
            InternalNote = InternalNote,
            Type = artefactType,
            PhysicalMediaType = mediaType,
            PhysicalMediaState = mediaState,
            FileName = FileName,
        };
    }

    public static Artefact FromDBEntity(Models.Digitalization.Artefact artefact)
    {
        return new() {
            Id = artefact.Id.ToString(),
            VersionIds = artefact.Versions.Select(v => v.Id.ToString()).ToList(),
            ParatextIds = artefact.Paratexts.Select(p => p.Id.ToString()).ToList(),

            FileName = artefact.FileName,
            InternalNote = artefact.InternalNote,
            WebsiteUrl = artefact.WebsiteUrl.ToString(),
            DigitalObjectType = artefact.DigitalObjectType,
            Format = artefact.Format,
            FileSize = artefact.FileSize,
            Quality = artefact.Quality,
            FedoraUrl = artefact.FedoraUrl,

            Type = artefact.Type.ToString(),
            PhysicalMediaType = artefact.PhysicalMediaType.ToString(),
            PhysicalMediaState = artefact.PhysicalMediaState.ToString(),
        };
    }
}
