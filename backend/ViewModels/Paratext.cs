namespace asec.ViewModels;

public record Paratext
{
    public string Id { get; set; }
    public string WorkId { get; set; }
    public string VersionId { get; set; }
    public string PackageId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Source { get; set; }
    public string SourceUrl { get; set; }
    public bool Downloadable { get; set; }
    public string Thumbnail { get; set; }

    public static Paratext FromDBParatext(Models.Archive.Paratext paratext)
    {
        return new Paratext() {
            Id = paratext.Id.ToString(),
            WorkId = paratext.Work?.Id.ToString(),
            VersionId = paratext.Version?.Id.ToString(),
            PackageId = paratext.GamePackage?.Id.ToString(),
            Name = paratext.Name,
            Description = paratext.Description,
            Source = paratext.Source,
            SourceUrl = paratext.SourceUrl,
            Downloadable = paratext.Downloadable,
            Thumbnail = paratext.Thumbnail
        };
    }
}