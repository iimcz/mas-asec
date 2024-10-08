using asec.Emulation;

namespace asec.ViewModels;

public record EmulationState(
    string Id,
    string PackageId,
    string Status,
    string StatusDetail,
    bool IsGpuPassthrough,
    bool IsUsbPassthrough
)
{
    public static EmulationState FromProcess(Process process)
    {
        return new EmulationState(
            process.Id.ToString(),
            process.PackageId.ToString(),
            process.Status.ToString(),
            process.StatusDetail,
            process.IsGpuPassthrough,
            process.IsUsbPassthrough
        );
    }
}
