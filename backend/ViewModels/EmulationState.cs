namespace asec.ViewModels;

public record EmulationState(
    string Id,
    string PackageId,
    string Status,
    string StatusDetail,
    bool IsGpuPassthrough,
    bool IsUsbPassthrough
);