using System.ComponentModel.DataAnnotations;

namespace asec.Models.Archive;

public class Paratext
{
    [Key]
    public Guid Id { get; set; }

    // Imported / exported data
    public int RemoteId { get; set; } = -1;
    public string Label { get; set; }
    public string Language { get; set; }
    public string Date { get; set; }
    public string InternalNote { get; set; }
    public string FilledOutBy { get; set; }
    public string WebsiteUrl { get; set; }
    public uint EmissionSize { get; set; }
    public string IdentificationNumber { get; set; }
    public string ParatextType { get; set; }

    // Generated data
    public DateTime ImportedAt { get; set; } = DateTime.Now;
    public DateTime ExportedAt { get; set; } = DateTime.MinValue;
    public bool CanExport { get; set; } = false;
    public bool Deleted { get; set; } = false;

    // Relationships
    public IList<DigitalObject> DigitalObjects { get; set; }
    public IList<PhysicalObject> PhysicalObjects { get; set; }
    public IList<WorkVersion> WorkVersions { get; set; }
}
