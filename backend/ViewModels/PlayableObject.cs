namespace asec.ViewModels;

public record PlayableObject {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Note { get; set; }
    public string EmulatorSlug { get; set; }
    public string Other { get; set; } // TODO: fill out according to what we will be parsing

    public static PlayableObject FromDBEntity(Models.Emulation.PlayableObject playableObject)
    {
        // TODO: implement
        return new();
    }
}
