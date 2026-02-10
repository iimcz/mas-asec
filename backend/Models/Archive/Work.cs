using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace asec.Models.Archive;

[Index(nameof(RemoteId))]
public class Work
{
    [Key]
    public Guid Id { get; set; }

    // Imported data
    public int RemoteId { get; set; }
    public string Label { get; set; }
    public string TypeOfWork { get; set; }
    public string InternalNote { get; set; }

    // Generated data
    public DateTime ImportedAt { get; set; } = DateTime.Now;

    // Relationships
    public IEnumerable<WorkVersion> Versions { get; set; }
}
