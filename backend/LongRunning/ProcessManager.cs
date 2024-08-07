using asec.LongRunning;

namespace asec.Digitalization;

public class ProcessManager<T> : IProcessManager<T> where T : IProcess
{
    private readonly Dictionary<Guid, ProcessRecord> _processes = new();

    public async Task CancelProcessAsync(Guid processId)
    {
        ProcessRecord record;
        if (!_processes.TryGetValue(processId, out record))
            throw new ArgumentException("Process not found", nameof(processId));
        record.TokenSource.Cancel();
        await record.Task.WaitAsync(CancellationToken.None);
    }

    public async Task<string> FinishProcessAsync(Guid processId)
    {
        ProcessRecord record;
        if (!_processes.TryGetValue(processId, out record))
            throw new ArgumentException("Process not found", nameof(processId));
        return await record.Task.WaitAsync(CancellationToken.None);
    }

    public T GetProcess(Guid processId)
    {
        ProcessRecord record;
        if (!_processes.TryGetValue(processId, out record))
            return default;
        return record.Process;
    }

    public void StartProcess(T process)
    {
        var tokenSource = new CancellationTokenSource();
        var processTask = Task.Run(() => process.Start(tokenSource.Token));

        _processes.Add(process.Id, new(process, tokenSource, processTask));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // do nothing
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop still running tasks
        foreach (var pair in _processes.Values)
        {
            pair.TokenSource.Cancel();
            await pair.Task.WaitAsync(cancellationToken);
        }
    }

    private record ProcessRecord(
        T Process,
        CancellationTokenSource TokenSource,
        Task<string> Task
    );
}