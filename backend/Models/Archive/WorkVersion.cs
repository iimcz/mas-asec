using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace asec.Models.Archive;

[Index(nameof(RemoteId))]
public class WorkVersion
{
    [Key]
    public Guid Id { get; set; }

    // Imported data
    public int RemoteId { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }
    public string Subtitle { get; set; }
    public string System { get; set; }
    public string CopyProtection { get; set; }
    public string CuratorialDescription { get; set; }

    // Generated data
    public DateTime ImportedAt { get; set; } = DateTime.Now;

    // Relationships
    public Work Work { get; set; }
    public IEnumerable<DigitalObject> DigitalObjects { get; set; }
    public IEnumerable<PhysicalObject> PhysicalObjects { get; set; }
    public IEnumerable<Paratext> Paratexts { get; set; }
    public IEnumerable<Digitalization.Artefact> Artefacts { get; set; }
    public IEnumerable<Emulation.GamePackage> GamePackages { get; set; }
}
