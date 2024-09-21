namespace asec.Models.Emulation;

public class Emulator
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Uri Homepage { get; set; }
    public IEnumerable<Platform> Platforms { get; set; }
    public IEnumerable<EmulationEnvironment> Environments { get; set; }
}