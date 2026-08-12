using asec.Models.Archive;

namespace asec.ViewModels;

public class DigitalObject
{
    public string Id { get; set; }
    public string Label { get; set; }
    public string Version { get; set; }
    public string FileName { get; set; }
    public string RepoUrl { get; set; }
    public DigitalObjectType DigitalObjectType { get; set; }
    public string Format { get; set; }
    public long FileSize { get; set; }
    public string MediaInfoReport { get; set; }
    public DateTime ImportedAt { get; set; }
    public DateTime ExportedAt { get; set; }
    public string InternalNote { get; set; }

    public static DigitalObject FromDBEntity(Models.Archive.DigitalObject digitalObject)
    {
        return new DigitalObject()
        {
            Id = digitalObject.Id.ToString(),
            Label = digitalObject.Label,
            Version = digitalObject.Version,
            FileName = digitalObject.FileName,
            RepoUrl = digitalObject.RepoUrl,
            DigitalObjectType = digitalObject.DigitalObjectType,
            Format = digitalObject.Format,
            FileSize = digitalObject.FileSize,
            MediaInfoReport = digitalObject.MediaInfoReport,
            ImportedAt = digitalObject.ImportedAt,
            ExportedAt = digitalObject.ExportedAt,
            InternalNote = digitalObject.InternalNote
        };
    }
}
