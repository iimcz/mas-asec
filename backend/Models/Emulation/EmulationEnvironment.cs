namespace asec.Models.Emulation;

public class EmulationEnvironment
{
    public Guid Id { get; set; }
    public Emulator Emulator { get; set; }
    public string EmulatorVersion { get; set; }
    public string EaasId { get; set; }
    public IEnumerable<Converter> Converters { get; set; }
}