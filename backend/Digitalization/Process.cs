using asec.Digitalization.Tools;

namespace asec.Digitalization;

public class Process
{

    public Guid Id { get; private set; } = Guid.NewGuid();
    public IDigitalizationTool DigitalizationTool { get; private set; }
    public CancellationToken CancellationToken { get; private set; }
    public DateTime StartTime { get; private set; }

    public string BaseDir { get; private set; }
    public string WorkDir { get; private set; }
    public string UploadDir { get; private set; }
    public string OutputPagh { get; private set; }
    public string LogPath { get; private set; }

    // TODO: consider locking modification to avoid changing the value during get from different thread
    public ProcessStatus Status { get; set; }
    public string StatusDetail { get; set; }


    public Process(IDigitalizationTool digitalizationTool, string dirsBase)
    {
        DigitalizationTool = digitalizationTool;
        BaseDir = Path.Combine(dirsBase, Id.ToString());
        WorkDir = Path.Combine(BaseDir, "work");
        UploadDir = Path.Combine(BaseDir, "upload");

        OutputPagh = Path.Combine(BaseDir, "output");
        LogPath = Path.Combine(BaseDir, "log.txt");

        CreateDirectoryStructure();
    }

    public async Task<string> WaitForInput()
    {
        if (Status != ProcessStatus.Running)
            throw new InvalidOperationException("Tried to wait when not running.");
        Status = ProcessStatus.WaitingForInput;


        Status = ProcessStatus.Running;

        // ...
        return null;
    }

    public async Task WaitForProcess()
    {
        if (Status != ProcessStatus.Running)
            throw new InvalidOperationException("Tried to wait when not running.");
        Status = ProcessStatus.WaitingForProcess;

        // ...

        Status = ProcessStatus.Running;
    }

    public Task<string> Start(CancellationToken cancellationToken)
    {
        StartTime = DateTime.Now;
        CancellationToken = cancellationToken;
        return DigitalizationTool.Start(this, cancellationToken);
    }
    
    private void CreateDirectoryStructure()
    {
        // Unnecessary in the current setup but for completeness
        Directory.CreateDirectory(BaseDir);

        Directory.CreateDirectory(WorkDir);
        Directory.CreateDirectory(UploadDir);
        
        File.Create(LogPath).Close();
    }
}