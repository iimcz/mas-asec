using asec.Models.Archive;

namespace asec.Models.Emulation;

public class GamePackage : DigitalObject
{
    public string Name { get; set; }
    public IEnumerable<DigitalObject> IncludedDigitalObjects { get; set; }
    public Archive.WorkVersion Version { get; set; }
    public EmulationEnvironment Environment { get; set; }
    public Converter Converter { get; set; }
    public DateTime ConversionDate { get; set; }
}
