namespace asec.LongRunning;

public interface IProcessManager<TProcess, TResult> : IHostedService where TProcess : IProcess<TResult>
{
    TProcess GetProcess(Guid processId);
    public List<TProcess> GetProcesses();
    void StartProcess(TProcess process);
    Task<TResult> StartProcessAsync(TProcess process);
    Task CancelProcessAsync(Guid processId);
    Task<TResult> FinishProcessAsync(Guid processId);
    void RemoveProcess(TProcess process);
}