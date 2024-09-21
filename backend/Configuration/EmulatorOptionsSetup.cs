using System.Text.Json;
using Microsoft.Extensions.Options;

namespace asec.Configuration;

public class EmulatorOptionsSetup : IConfigureOptions<EmulatorOptions>
{
    private string _emulatorsFilePath;

    public EmulatorOptionsSetup(IConfiguration config)
    {
        _emulatorsFilePath = config.GetSection("Emulation").GetValue<string>("EmulatorsConfigPath");
    }
    public void Configure(EmulatorOptions options)
    {
        using (FileStream stream = new FileStream(_emulatorsFilePath, FileMode.Open))
        {
            var loaded = JsonSerializer.Deserialize<EmulatorOptions>(stream);
            options.Configurations = loaded?.Configurations;
        }
    }
}