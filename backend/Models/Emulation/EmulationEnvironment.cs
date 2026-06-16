namespace asec.Models.Emulation;

public class EmulationEnvironment
{
    public Guid Id { get; set; }
    public string Slug { get; set; }
    public string Name { get; set; }
    public string Note { get; set; }
    public Emulator Emulator { get; set; }
    public string Version { get; set; }
    public string EaasId { get; set; }
    public bool InternetConnected { get; set; }
    public IList<Converter> Converters { get; set; }
    public EnvironmentType Type { get; set; }
}
