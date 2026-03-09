namespace asec.Models.Emulation;

public class Emulator
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Uri Homepage { get; set; }
    public IList<Platform> Platforms { get; set; }
    public IList<EmulationEnvironment> Environments { get; set; }
}
