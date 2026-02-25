using System.ComponentModel.DataAnnotations;

namespace asec.Models.Archive;

public class Paratext
{
    [Key]
    public Guid Id { get; set; }

    // Imported / exported data
    public string Language { get; set; } 
    public DateOnly Date { get; set; }
    public string InternalNote { get; set; }
    public string FilledOutBy { get; set; }
    public Uri WebsiteUrl { get; set; }
    public uint EmissionSize { get; set; }
    public string IdentificationNumber { get; set; }
    public string ParatextType { get; set; }

    // Generated data
    public DateTime ImportedAt { get; set; } = DateTime.Now;
    public DateTime ExportedAt { get; set; } = DateTime.MinValue;

    // Relationships
    public DigitalObject DigitalObject { get; set; }
    public PhysicalObject PhysicalObject { get; set; }
    public IEnumerable<WorkVersion> WorkVersions { get; set; }
}
