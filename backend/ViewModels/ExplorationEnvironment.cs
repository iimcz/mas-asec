namespace asec.ViewModels;

public record ExplorationEnvironment
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Version { get; set; }
    public string Note { get; set; }

    public static ExplorationEnvironment FromEmulationEnvironment(asec.Models.Emulation.EmulationEnvironment environment)
    {
        return new()
        {
            Id = environment.Id.ToString(),
            Name = environment.Name,
            Slug = environment.Slug,
            Version = environment.Version,
            Note = environment.Note
        };
    }
}
