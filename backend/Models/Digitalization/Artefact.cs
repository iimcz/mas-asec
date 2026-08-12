using asec.Models.Archive;

namespace asec.Models.Digitalization;

public enum ArtefactType
{
    IsoImage,
    ZipArchive,
    SfmFloppy,
    WindowsBinary,
    LinuxBinary,
    WavAudio,
    Unknown,
}

public enum PhysicalMediaType
{
    Floppy35,
    Floppy54,
    CD,
    DVD,
    Flash,
    AudioCassette,
    None,
    Unknown
}

public class Artefact : DigitalObject
{
    public DateTime ArchivationDate { get; set; }
    public ArtefactType Type { get; set; }
    public DigitalizationTool DigitalizationTool { get; set; }
    public PhysicalMediaType PhysicalMediaType { get; set; }
}
