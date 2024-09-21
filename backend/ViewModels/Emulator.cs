using asec.Models.Emulation;

namespace asec.ViewModels;

public record Emulator
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Homepage { get; set; }
    public string Version { get; set; }
    public IEnumerable<string> Platforms { get; set; }

    public static Emulator FromEmulationEnvironment(EmulationEnvironment environment)
    {
        if (environment.Emulator == null)
            return null;
        
        return new() {
            Id = environment.Id.ToString(),
            Name = environment.Emulator.Name,
            Homepage = environment.Emulator.Homepage.ToString(),
            Version = environment.EmulatorVersion,
            Platforms = environment.Emulator.Platforms.Select(p => p.Name)
        };
    }
}