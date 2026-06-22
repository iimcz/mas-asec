using asec.Models.Emulation;

namespace asec.Exploration;

public class ExplorationResult
{
    public PlayableObject PlayableObject { get; set; }
    public List<Guid> IncludedArtefactIds { get; set; }
    public Guid VersionId { get; set; }
}
