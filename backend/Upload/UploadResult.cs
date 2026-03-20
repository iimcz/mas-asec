using asec.Models.Digitalization;

namespace asec.Upload
{
    public record UploadResult(
        string Filename,
        ArtefactType Type
    );
}
