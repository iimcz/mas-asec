using asec.Models.Archive;
using asec.Models.Digitalization;

namespace asec.Models.Emulation;

public class GamePackage
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public IEnumerable<Artefact> IncludedArtefacts { get; set; }
    public Archive.WorkVersion Version { get; set; }
    public EmulationEnvironment Environment { get; set; }
    public Converter Converter { get; set; }
    public DateTime ConversionDate { get; set; }
    public IEnumerable<Paratext> Paratexts { get; set; }
}
