using asec.Digitalization.Tools;

namespace asec.Digitalization;

public interface IProcessManager : IHostedService
{
    Process GetProcess(Guid processId);
    Process StartProcess(IDigitalizationTool tool);
    Task CancelProcess(Guid processId);
    Task FinishProcess(Guid processId);
}