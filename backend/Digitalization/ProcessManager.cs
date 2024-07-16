
using asec.Digitalization.Tools;

namespace asec.Digitalization;

public class ProcessManager : IProcessManager
{
    private readonly string _processBaseDir;
    private Dictionary<Guid, ProcessRecord> _processes = new();

    public ProcessManager(IConfiguration config)
    {
        _processBaseDir = config.GetSection("Digitalization").GetValue<string>("ProcessBaseDir");
    }

    public async Task CancelProcess(Guid processId)
    {
        ProcessRecord record;
        if (!_processes.TryGetValue(processId, out record))
            throw new ArgumentException("Process not found", nameof(processId));
        record.TokenSource.Cancel();
        await record.Task.WaitAsync(CancellationToken.None);
    }

    public async Task FinishProcess(Guid processId)
    {
        ProcessRecord record;
        if (!_processes.TryGetValue(processId, out record))
            throw new ArgumentException("Process not found", nameof(processId));
        await record.Task.WaitAsync(CancellationToken.None);
    }

    public Process GetProcess(Guid processId)
    {
        ProcessRecord record;
        if (!_processes.TryGetValue(processId, out record))
            return null;
        return record.Process;
    }

    public Process StartProcess(IDigitalizationTool tool)
    {
        var process = new Process(tool, _processBaseDir);
        var tokenSource = new CancellationTokenSource();
        var processTask = Task.Run(() => process.Start(tokenSource.Token));

        _processes.Add(process.Id, new(process, tokenSource, processTask));
        return process;
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
        Process Process,
        CancellationTokenSource TokenSource,
        Task Task
    );
}