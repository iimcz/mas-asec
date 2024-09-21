using System.Reactive.Threading.Tasks;
using System.Text.Encodings.Web;
using asec.LongRunning;

namespace asec.Digitalization;

public class ProcessManager<TProcess, TResult> : IProcessManager<TProcess, TResult> where TProcess : IProcess<TResult>
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

    public async Task<TResult> FinishProcessAsync(Guid processId)
    {
        ProcessRecord record;
        if (!_processes.TryGetValue(processId, out record))
            throw new ArgumentException("Process not found", nameof(processId));
        return await record.Task.WaitAsync(CancellationToken.None);
    }

    public TProcess GetProcess(Guid processId)
    {
        ProcessRecord record;
        if (!_processes.TryGetValue(processId, out record))
            return default;
        if (record.Task.Exception != null)
            throw record.Task.Exception;
        return record.Process;
    }

    public void StartProcess(TProcess process)
    {
        var tokenSource = new CancellationTokenSource();
        var processTask = Task.Run(() => process.Start(tokenSource.Token));

        _processes.Add(process.Id, new(process, tokenSource, processTask));
    }

    public void RemoveProcess(TProcess process)
    {
        if (!_processes.ContainsKey(process.Id))
            return;
        if (process.Status != ProcessStatus.Success && process.Status != ProcessStatus.Failed)
            throw new InvalidOperationException("Cannot remove still running process!");
        _processes.Remove(process.Id);
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
        TProcess Process,
        CancellationTokenSource TokenSource,
        Task<TResult> Task
    );
}