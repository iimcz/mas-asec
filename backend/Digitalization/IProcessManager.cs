using asec.Digitalization.Tools;

namespace asec.Digitalization;

public interface IProcessManager : IHostedService
{
    Process GetProcess(Guid processId);
    Process StartProcess(IDigitalizationTool tool, Models.Archive.Version version);
    Task CancelProcessAsync(Guid processId);
    Task<string> FinishProcessAsync(Guid processId);
}