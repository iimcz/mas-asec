namespace asec.ViewModels;

public record DigitalizationProcess
{
    public string ProcessId { get; set; } = "";
    public string ToolId { get; set; } = "";
    public string Status { get; set; } = "";
    public string StatusDetail { get; set; } = "";
    public string StartTime { get; set; } = "";

    public static DigitalizationProcess FromProcess(Digitalization.Process process)
    {
        return new DigitalizationProcess {
            ProcessId = process.Id.ToString(),
            ToolId = process.DigitalizationTool.Id.ToString(),
            Status = process.Status.ToString(),
            StatusDetail = process.StatusDetail,
            StartTime = process.StartTime.ToString()
        };
    }
}