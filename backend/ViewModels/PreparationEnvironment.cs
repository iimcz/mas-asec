namespace asec.ViewModels;

public record PreparationEnvironment
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string Note { get; set; }

    public static PreparationEnvironment FromEmulationEnvironment(asec.Models.Emulation.EmulationEnvironment environment)
    {
        return new()
        {
            Id = environment.Id.ToString(),
            Name = environment.Name,
            Version = environment.Version,
            Note = environment.Note
        };
    }
}
