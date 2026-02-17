namespace asec.ViewModels;

public record WorkVersion
{
    public string Id { get; set; }
    public string WorkId { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }
    public string Subtitle { get; set; }
    public string System { get; set; }
    public string CopyProtection { get; set; }
    public string CuratorialDescription { get; set; }
    public string InternalNote { get; set; }
    public DateTime ImportedAt { get; set; }

    public static WorkVersion FromDBEntity(Models.Archive.WorkVersion dbVersion)
    {
        return new() {
            Id = dbVersion.Id.ToString(),
            WorkId = dbVersion.Work?.Id.ToString(),
            Label = dbVersion.Label,
            Description = dbVersion.Description,
            Subtitle = dbVersion.Subtitle,
            System = dbVersion.System,
            CopyProtection = dbVersion.CopyProtection,
            CuratorialDescription = dbVersion.CuratorialDescription,
            InternalNote = dbVersion.InternalNote,
            ImportedAt = dbVersion.ImportedAt
        };
    }
}
