namespace asec.ViewModels;

public record Paratext
{
    public string Id { get; set; }
    public string Language { get; set; } 
    public DateOnly Date { get; set; }
    public string InternalNote { get; set; }
    public string FilledOutBy { get; set; }
    public Uri WebsiteUrl { get; set; }
    public uint EmissionSize { get; set; }
    public string IdentificationNumber { get; set; }
    public string ParatextType { get; set; }
    public DateTime ImportedAt { get; set; }
    public DateTime ExportedAt { get; set; }

    public static Paratext FromDBParatext(Models.Archive.Paratext paratext)
    {
        return new Paratext() {
            Id = paratext.Id.ToString(),
            Language = paratext.Language,
            Date = paratext.Date,
            InternalNote = paratext.InternalNote,
            FilledOutBy = paratext.FilledOutBy,
            WebsiteUrl = paratext.WebsiteUrl,
            EmissionSize = paratext.EmissionSize,
            IdentificationNumber = paratext.IdentificationNumber,
            ParatextType = paratext.ParatextType,
            ImportedAt = paratext.ImportedAt,
            ExportedAt = paratext.ExportedAt
        };
    }
}
