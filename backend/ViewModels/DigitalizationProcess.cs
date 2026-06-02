namespace asec.ViewModels;

public record DigitalizationDetail(
    string ToolId,
    string ToolMessage
);

public record DigitalizationProcess : Process<DigitalizationDetail>
{
    public static DigitalizationProcess FromProcess(Digitalization.Process process)
    {
        return new DigitalizationProcess
        {
            Id = process.Id.ToString(),
            StartTime = process.StartTime.ToString(),
            Status = process.Status.ToString(),
            StatusDetail = new(
                process.DigitalizationTool.Id.ToString(),
                process.StatusDetail.ToolMessage
            )
        };
    }
}
