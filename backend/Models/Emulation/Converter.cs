using asec.Models.Digitalization;

namespace asec.Models.Emulation;

public class Converter
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string Environment { get; set; }
    public IList<ArtefactType> SupportedArtefactTypes { get; set; }
    public string Configuration { get; set; }
}