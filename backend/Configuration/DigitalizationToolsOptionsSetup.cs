using System.Text.Json;
using Microsoft.Extensions.Options;

namespace asec.Configuration;

public class DigitalizationToolsOptionsSetup : IConfigureOptions<DigitalizationToolsOptions>
{
    private string _toolsFilePath;

    public DigitalizationToolsOptionsSetup(IConfiguration config)
    {
        _toolsFilePath = config.GetSection("Digitalization").GetValue<string>("ToolsConfigPath");
    }

    public void Configure(DigitalizationToolsOptions options)
    {
        using (FileStream stream = new FileStream(_toolsFilePath, FileMode.Open))
        {
            var loaded = JsonSerializer.Deserialize<DigitalizationToolsOptions>(stream);
            options.Configurations = loaded?.Configurations;
        }
    }
}