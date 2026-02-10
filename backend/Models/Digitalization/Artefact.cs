using System.ComponentModel.DataAnnotations;

namespace asec.Models.Digitalization;

public class Artefact
{
    [Key]
    public Guid Id { get; set; }
    public Archive.WorkVersion Version { get; set; }
    public string Name { get; set; }
    public string PhysicalMediaState { get; set; }
    public DateTime ArchivationDate { get; set; }
    public string Archiver { get; set; }
    public string Note { get; set; }
    public ArtefactType Type { get; set; }
    public PhysicalMediaType PhysicalMediaType { get; set; }
    public string OriginalFilename { get; set; }
    public DigitalizationTool DigitalizationTool { get; set; }
}
