using asec.Models.Archive;

namespace asec.Models.Emulation;

public class PlayableObject : DigitalObject
{
    public DateTime CreationDate { get; set; }
    public IList<DigitalObject> IncludedDigitalObjects { get; set; }
    public EmulationEnvironment Environment { get; set; }
}
