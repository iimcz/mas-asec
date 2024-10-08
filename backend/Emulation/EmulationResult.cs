namespace asec.Emulation
{
    public enum RecordingType
    {
        Screen,
        Webcam
    }

    public record VideoFile
    (
        string Path,
        RecordingType Type
    );

    public record EmulationResult
    (
        List<VideoFile> VideoFiles,
        string SnapshotId
    );
}