namespace asec.LongRunning;

public interface IProcessManager<TProcess, TResult, TDetail> : IHostedService where TProcess : IProcess<TResult, TDetail>
{
    TProcess GetProcess(Guid processId);
    void StartProcess(TProcess process);
    Task<TResult> StartProcessAsync(TProcess process);
    Task CancelProcessAsync(Guid processId);
    Task<TResult> FinishProcessAsync(Guid processId);
    void RemoveProcess(TProcess process);
}
