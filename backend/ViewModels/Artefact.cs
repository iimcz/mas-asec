using asec.Models;
using asec.Models.Digitalization;

namespace asec.ViewModels;

public record ArtefactListOptions
{
    public List<string> Formats { get; set; }
}

public record ArtefactUpdate
{
    public string Label { get; set; }
    public string Version { get; set; }
    public string Format { get; set; }
    public string InternalNote { get; set; }

    public string Type { get; set; }
    public string PhysicalMediaType { get; set; }

    public async Task<Models.Digitalization.Artefact> ToDBEntity(AsecDBContext context)
    {
        var artefactType = ArtefactType.Unknown;
        if (Type != null)
            Enum.TryParse(Type, true, out artefactType);

        return new() {
            Id = Guid.Empty,
            Label = Label,
            Version = Version,
            Format = Format,
            InternalNote = InternalNote,
            ArchivationDate = DateTime.Now,
            Type = artefactType
        };
    }
}

public record Artefact
{
    public string Id { get; set; }
    public string Label { get; set; }
    public string Version { get; set; }
    public string FileName { get; set; }
    public string RepoUrl { get; set; }
    public string DigitalObjectType { get; set; }
    public string Format { get; set; }
    public long FileSize { get; set; }
    public string MediaInfoReport { get; set; }
    public string InternalNote { get; set; }

    public string Type { get; set;}
    public string PhysicalMediaType { get; set; }

    public List<string> ParatextIds { get; set; }
    public List<string> VersionIds { get; set; }

    public static Artefact FromDBEntity(Models.Digitalization.Artefact artefact)
    {
        return new() {
            Id = artefact.Id.ToString(),
            VersionIds = artefact.WorkVersions?.Select(v => v.Id.ToString()).ToList(),
            ParatextIds = artefact.Paratexts?.Select(p => p.Id.ToString()).ToList(),

            Label = artefact.Label,
            Version = artefact.Version,
            FileName = artefact.FileName,
            RepoUrl = artefact.RepoUrl,
            DigitalObjectType = artefact.DigitalObjectType.ToString(),
            Format = artefact.Format,
            FileSize = artefact.FileSize,
            MediaInfoReport = artefact.MediaInfoReport,
            InternalNote = artefact.InternalNote,

            Type = artefact.Type.ToString(),
            PhysicalMediaType = artefact.PhysicalMediaType.ToString()
        };
    }
}
