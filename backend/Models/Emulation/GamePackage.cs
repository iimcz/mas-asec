using asec.Models.Archive;

namespace asec.Models.Emulation;

public class GamePackage : DigitalObject
{
    public string ObjectId { get; set; }
    public string Name { get; set; }
    public IList<DigitalObject> IncludedDigitalObjects { get; set; }
    public Archive.WorkVersion Version { get; set; }
    public EmulationEnvironment Environment { get; set; }
    public Converter Converter { get; set; }
    public DateTime ConversionDate { get; set; }
}
