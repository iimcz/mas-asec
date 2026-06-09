using asec.Emulation;

namespace asec.Configuration;

public class EmulatorOptions
{
    public ICollection<EmulatorConfig> Emulators { get; set; }
    public ICollection<EmulatorEnvironmentConfig> ExplorationEnvironments { get; set; }
}
