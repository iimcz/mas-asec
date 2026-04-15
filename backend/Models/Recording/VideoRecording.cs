using asec.Emulation;
using asec.Models.Archive;

namespace asec.Models.Recording;

public class VideoRecording : DigitalObject
{
    public RecordingType RecordingType { get; set; }
}