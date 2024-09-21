namespace asec.ViewModels;

public record GamePackage
{
    public string Id { get; set; } = "";
    public List<string> IncludedArtefactIds { get; set; } = new List<string>();
    public string VersionId { get; set; } = "";
    public string EmulatorId { get; set; } = "";
    public string ConversionDate { get; set; } = "";

    public static GamePackage FromGamePackage(Models.Emulation.GamePackage package)
    {
        return new() {
            Id = package.Id.ToString(),
            IncludedArtefactIds = package.IncludedArtefacts?.Select(a => a.Id.ToString()).ToList(),
            VersionId = package.Version?.Id.ToString(),
            EmulatorId = package.Environment?.Id.ToString(),
            ConversionDate = package.ConversionDate.ToString()
        };
    }
}