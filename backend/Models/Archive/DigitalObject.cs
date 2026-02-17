namespace asec.Models.Archive;

using System.ComponentModel.DataAnnotations;

public class DigitalObject
{
    [Key]
    public Guid Id { get; set; }

    // Imported / saved data
    public int RemoteId { get; set; }
    public string InternalNote { get; set; }
    public Uri WebsiteUrl { get; set; }
    public string DigitalObjectType { get; set; }
    public string Format { get; set; }
    public uint FileSize { get; set; }
    public string Quality { get; set; }
    public string FedoraUrl { get; set; }
    public string FileName { get; set; }

    // Generated
    public DateTime ImportedAt { get; set; } = DateTime.Now;
    public DateTime ExportedAt { get; set; } = DateTime.MinValue;

    // Relationships
    public IEnumerable<WorkVersion> Versions { get; set; }
    public IEnumerable<Paratext> Paratexts { get; set; }
}
