namespace asec.ViewModels;

public record Platform {
    public string Name { get; set; }
    public List<string> PhysicalMediaTypes { get; set; }

    public static Platform FromPlatform(Models.Emulation.Platform platform)
    {
        return new() {
            Name = platform.Name,
            PhysicalMediaTypes = platform.MediaTypes.Select(mt => mt.ToString()).ToList()
        };
    }
}