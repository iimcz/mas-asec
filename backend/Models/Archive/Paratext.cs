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
    public DateOnly Date { get; set; }
    public string InternalNote { get; set; }
    public string FilledOutBy { get; set; }
    public string WebsiteUrl { get; set; }
    public uint EmissionSize { get; set; }
    public string IdentificationNumber { get; set; }
    public string ParatextType { get; set; }

    // Generated data
    public DateTime ImportedAt { get; set; } = DateTime.Now;
    public DateTime ExportedAt { get; set; } = DateTime.MinValue;
    public bool Deleted { get; set; } = false;

    // Relationships
    public DigitalObject DigitalObject { get; set; }
    public PhysicalObject PhysicalObject { get; set; }
    public IList<WorkVersion> WorkVersions { get; set; }
}
