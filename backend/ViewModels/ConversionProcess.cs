using asec.DataConversion;

namespace asec.ViewModels;

public record ConversionProcess
{
    public string ProcessId { get; set; } = "";
    public string EmulatorId { get; set; } = "";
    public string Status { get; set; } = "";
    public string StatusDetail { get; set; } = "";
    public string StartTime { get; set; } = "";
    public List<string> ArtefactIds { get; set; } = new List<string>();

    public static ConversionProcess FromProcess(Process process)
    {
        var artefactIds = process.Artefacts.Select(a => a.Id.ToString());
        var result = new ConversionProcess() {
            ProcessId = process.Id.ToString(),
            EmulatorId = process.EnvironmentId.ToString(),
            Status = process.Status.ToString(),
            StatusDetail = process.StatusDetail,
            StartTime = process.StartTime.ToString()
        };
        result.ArtefactIds.AddRange(artefactIds);
        return result;
    }
}