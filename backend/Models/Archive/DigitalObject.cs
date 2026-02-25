namespace asec.Models.Archive;

using System.ComponentModel.DataAnnotations;

public class DigitalObject
{
    [Key]
    public Guid Id { get; set; }

    // Imported / exported data
    public int RemoteId { get; set; } = -1;
    public string FileName { get; set; }
    public string InternalNote { get; set; }
    public Uri WebsiteUrl { get; set; }
    public string DigitalObjectType { get; set; }
    public string Format { get; set; }
    public uint FileSize { get; set; }
    public string Quality { get; set; }
    public string FedoraUrl { get; set; }

    // Generated
    public DateTime ImportedAt { get; set; } = DateTime.Now;
    public DateTime ExportedAt { get; set; } = DateTime.MinValue;

    // Relationships
    public IEnumerable<WorkVersion> WorkVersions { get; set; }
    public IEnumerable<Paratext> Paratexts { get; set; }
    public PhysicalObject PhysicalObject { get; set; }
}
