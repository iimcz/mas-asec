using asec.Models.Archive;

namespace asec.Models.Digitalization;

public class Artefact : DigitalObject
{
    // Local DB data
    public Guid ObjectId { get; set; }
    public PhysicalMediaState PhysicalMediaState { get; set; }
    public DateTime ArchivationDate { get; set; }
    public ArtefactType Type { get; set; }
    public PhysicalMediaType PhysicalMediaType { get; set; }
    public DigitalizationTool DigitalizationTool { get; set; }
}
