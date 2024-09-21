using System.Text.Json.Serialization;
using asec.DataConversion.Converters;

namespace asec.Emulation;

public class EmulatorConfig
{
    public string Name { get; set; }
    public Uri Homepage { get; set; }
    public IList<string> Platforms { get; set; }
    public IList<EmulatorEnvironmentConfig> Environments { get; set; }
}

public class EmulatorEnvironmentConfig
{
    public string Version { get; set; }
    public string EaasId { get; set; }
    public IList<ConverterConfig> Converters { get; set; }
}