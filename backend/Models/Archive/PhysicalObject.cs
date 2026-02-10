namespace asec.Models.Archive;

using System.ComponentModel.DataAnnotations;

public class PhysicalObject
{
    [Key]
    public Guid Id { get; set; }

    // Imported / exported data
    public string Description { get; set; }
    public DateOnly Date { get; set; }
    public string InternalNote { get; set; }
    public string FilledOutBy { get; set; }
    public string PhysicalObjectType { get; set; }
    public string CountryOfOrigin { get; set; }
    public Uri WebsiteUrl { get; set; }
    public string EAN { get; set; }
    public string ISBN { get; set; }
    public string Condition { get; set; }
    public string Location { get; set; }
    public string Size { get; set; }
    public string Owner { get; set; }

    // Generated data
    public DateTime ImportedAt { get; set; } = DateTime.Now;
    public DateTime ExportedAt { get; set; } = DateTime.MinValue;

    // Relationships
    public IEnumerable<WorkVersion> Versions { get; set; }
    public IEnumerable<Paratext> Paratexts { get; set; }
}
