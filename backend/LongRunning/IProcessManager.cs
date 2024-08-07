namespace asec.LongRunning;

public interface IProcessManager<T> : IHostedService where T : IProcess
{
    T GetProcess(Guid processId);
    void StartProcess(T process);
    Task CancelProcessAsync(Guid processId);
    Task<string> FinishProcessAsync(Guid processId);
}