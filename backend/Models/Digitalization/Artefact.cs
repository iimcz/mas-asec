using System.ComponentModel.DataAnnotations;

namespace asec.Models.Digitalization;

public class Artefact
{
    [Key]
    public Guid Id { get; set; }
    public Archive.Version Version { get; set; }
    public string Name { get; set; }
    public string PhysicalMediaState { get; set; }
    public DateTime ArchivationDate { get; set; }
    public string Archiver { get; set; }
    public string Note { get; set; }
}