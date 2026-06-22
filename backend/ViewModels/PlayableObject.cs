namespace asec.ViewModels;

public record PlayableObject {
    public string Id { get; set; }
    public string Label { get; set; }
    public string Note { get; set; }
    public string EmulatorSlug { get; set; }
    public string CreationDate { get; set; }
    public string VersionId { get; set; }
    public List<string> DigitalObjectIds { get; set; }

    public static PlayableObject FromDBEntity(Models.Emulation.PlayableObject playableObject)
    {
        return new() {
            Id = playableObject.Id.ToString(),
            Label = playableObject.Label,
            Note = playableObject.InternalNote,
            EmulatorSlug = playableObject.Environment?.Slug.ToString(),
            VersionId = playableObject.WorkVersions?.FirstOrDefault()?.Id.ToString(),
            CreationDate = playableObject.CreationDate.ToString(),
            DigitalObjectIds = playableObject.IncludedDigitalObjects?.Select(o => o.Id.ToString()).ToList(),
        };
    }
}
