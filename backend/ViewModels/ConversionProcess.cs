using asec.DataConversion;

namespace asec.ViewModels;

public record ConversionDetail(
    string EmulatorId,
    List<string> ArtefactIds,
    string ToolMessage
);

public record ConversionProcess : Process<ConversionDetail>
{
    public static ConversionProcess FromProcess(Process process)
    {
        var artefactIds = process.Artefacts.Select(a => a.Id.ToString());
        var result = new ConversionProcess()
        {
            Id = process.Id.ToString(),
            StartTime = process.StartTime.ToString(),
            Status = process.Status.ToString(),
            StatusDetail = new(
                process.EnvironmentId.ToString(),
                artefactIds.ToList(),
                process.StatusDetail.ToolMessage
            )
        };
        return result;
    }
}
