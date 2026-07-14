namespace asec.Models.Archive;

using System.ComponentModel.DataAnnotations;

public enum DigitalObjectType
{
    GameArtefact,
    PlayableObject,
    Modification,
    UnplayableParatext
}

public class DigitalObject
{
    [Key]
    public Guid Id { get; set; }

    // Imported / exported data
    public int RemoteId { get; set; } = -1;
    public string Label { get; set; }
    public string Version { get; set; }
    public string FileName { get; set; }
    public string RepoUrl { get; set; }
    public DigitalObjectType DigitalObjectType { get; set; }
    public string Format { get; set; }
    public long FileSize { get; set; }
    public string MediaInfoReport { get; set; }
    public string InternalNote { get; set; }

    // Generated
    public DateTime ImportedAt { get; set; } = DateTime.Now;
    public DateTime ExportedAt { get; set; } = DateTime.MinValue;

    // Relationships
    public IList<WorkVersion> WorkVersions { get; set; }
    public IList<Paratext> Paratexts { get; set; }
    public PhysicalObject PhysicalObject { get; set; }
}
