using asec.Models.Archive;

namespace asec.Models.Emulation;

public class PlayableObject : DigitalObject
{
    public string ObjectId { get; set; }
    public DateTime CreationDate { get; set; }
    public IList<DigitalObject> IncludedDigitalObjects { get; set; }
    public WorkVersion Version { get; set; }
    public EmulationEnvironment Environment { get; set; }
}
