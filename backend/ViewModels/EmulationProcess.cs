using asec.Emulation;

namespace asec.ViewModels;

public record EmulationDetail(
    bool IsGpuPassthrough,
    bool IsUsbPassthrough,
    string Other
);

public record EmulationProcess : Process<EmulationDetail>
{
    public static EmulationProcess FromProcess(BaseProcess process)
    {
        return new EmulationProcess
        {
            Id = process.Id.ToString(),
            StartTime = process.StartTime.ToString(),
            Status = process.Status.ToString(),
            StatusDetail = new(
                process.StatusDetail.IsGpuPassthrough,
                process.StatusDetail.IsUsbPassthrough,
                process.StatusDetail.Other
            )
        };
    }
}
