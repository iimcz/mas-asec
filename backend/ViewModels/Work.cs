namespace asec.ViewModels;

public record Work
{
    public string Id { get; set; }
    public string Label { get; set; }
    public string TypeOfWork { get; set; }
    public string CuratorialDescription { get; set; }
    public string InternalNote { get; set; }
    public DateTime ImportedAt { get; set; }

    public static Work FromDbEntity(asec.Models.Archive.Work dbWork)
    {
        return new() {
            Id = dbWork.Id.ToString(),
            Label = dbWork.Label,
            TypeOfWork = dbWork.TypeOfWork,
            CuratorialDescription = dbWork.CuratorialDescription,
            InternalNote = dbWork.InternalNote,
            ImportedAt = dbWork.ImportedAt
        };
    }
}
