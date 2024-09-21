using System.Text.Json;
using System.Text.Json.Serialization;
using asec.Models;
using asec.Models.Digitalization;
using asec.Models.Emulation;

namespace asec.Extensions;

public static class PlatformLoader
{
    public static void LoadPlatforms(this WebApplication app)
    {
        app.Logger.LogInformation("Loading platforms from configuration.");
        var emulationSection = app.Configuration.GetSection("Emulation");

        var platformsConfigPath = emulationSection.GetValue<string>("PlatformsConfigPath");
        if (string.IsNullOrEmpty(platformsConfigPath) || !File.Exists(platformsConfigPath))
            throw new Exception($"Missing platform configuration file {platformsConfigPath}");
        var platforms = new List<Platform>();
        using (var stream = new FileStream(platformsConfigPath, FileMode.Open))
        {
            // TODO: when we get to .NET 8, use the generic variant for AOT support
            var enumConverter = new JsonStringEnumConverter();
            var options = new JsonSerializerOptions();
            options.Converters.Add(enumConverter);
            platforms = JsonSerializer.Deserialize<List<Platform>>(stream, options)!;
        }

        app.Logger.LogInformation("Merging {} loaded platforms into database.", platforms.Count);
        using var serviceScope = app.Services.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<AsecDBContext>();
        foreach (var platform in platforms) {
            var dbPlatform = dbContext.Platforms.Find(platform.Name);
            if (dbPlatform == null)
            {
                app.Logger.LogInformation("Adding platform {}", platform.Name);
                dbContext.Platforms.Add(
                    new() {
                        Name = platform.Name,
                        MediaTypes = platform.MediaTypes
                    }
                );
            }
            else
            {
                app.Logger.LogInformation("Updating platform {}", platform.Name);
                dbPlatform.MediaTypes = platform.MediaTypes;
            }
        }
        dbContext.SaveChanges();
    }
}