using System.Diagnostics;
using System.Text.RegularExpressions;
using asec.LongRunning;
using asec.Models.Digitalization;

namespace asec.Digitalization.Tools;

public class GreaseweazleToolConfig : DigitalizationToolConfig
{
    public string GWPath { get; set; }
    public string DevicePath { get; set; }
    public string Drive { get; set; }
    public PhysicalMediaType MediaType { get; set; }

    public override IDigitalizationTool ConstructTool()
    {
        return new GreaseweazleTool(this);
    }
}

public class GreaseweazleTool : IDigitalizationTool
{
    private readonly Regex ToolVersionRegex = new Regex(@"^Host Tools: (\d+).(\d+)$");
    private readonly Regex FirmwareVersionRegex = new Regex(@"^\s*Firmware: (\d+).(\d+)$");

    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Slug => _config.Slug;

    public string Name => "Greaseweazle";

    public string Version { get; private set; } = "";

    public string Environment {
        get {
            string OS = System.Environment.OSVersion.VersionString;
            string CLR = System.Environment.Version.ToString();
            return $"OS:{OS},CLR:{CLR}";
        }
    }

    public PhysicalMediaType PhysicalMedia => _config.MediaType;

    public bool IsAvailable { get; private set; } = false;

    private readonly GreaseweazleToolConfig _config;

    public GreaseweazleTool(GreaseweazleToolConfig config)
    {
        _config = config;
        if (_config.MediaType != PhysicalMediaType.Floppy35 &&
            _config.MediaType != PhysicalMediaType.Floppy54)
            throw new ArgumentException("Invalid physical media type: {}", nameof(_config.MediaType));
    }

    public async Task Initialize(CancellationToken cancellationToken)
    {
        if (Path.Exists(_config.GWPath))
        {
            ProcessStartInfo gwProcessInfo = new() {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = _config.GWPath
            };
            gwProcessInfo.ArgumentList.Add("info");
            gwProcessInfo.ArgumentList.Add("--device");
            gwProcessInfo.ArgumentList.Add(_config.DevicePath);
            var gwProcess = System.Diagnostics.Process.Start(gwProcessInfo);
            if (gwProcess == null)
                return;
            await gwProcess.WaitForExitAsync(cancellationToken);
            if (!gwProcess.HasExited || gwProcess.ExitCode != 0)
                return;

            bool haveHostVersion = false;
            while (!gwProcess.StandardError.EndOfStream)
            {
                var line = gwProcess.StandardError.ReadLine() ?? "";
                if (!haveHostVersion)
                {
                    var match = ToolVersionRegex.Match(line);
                    if (!match.Success) continue;

                    Version = $"{match.Groups[1]}.{match.Groups[2]}";
                    haveHostVersion = true;
                }
                else
                {
                    var match = FirmwareVersionRegex.Match(line);
                    if (!match.Success) continue;

                    Version += $"+{match.Groups[1]}.{match.Groups[2]}";
                    IsAvailable = true;
                    break;
                }
            }
        }
    }

    public async Task<DigitalizationResult> Start(Process process, CancellationToken cancellationToken)
    {
        using var logStream = new FileStream(process.LogPath, FileMode.Append);
        using var logWriter = new StreamWriter(logStream);
        logWriter.AutoFlush = true;

        void logCallback(object sender, DataReceivedEventArgs e)
        {
            logWriter.WriteLine(e.Data);
        }

        await logWriter.WriteLineAsync("Checking the floppy drive...");
        var checkProcessInfo = new ProcessStartInfo() {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            FileName = _config.GWPath,
        };
        checkProcessInfo.ArgumentList.Add("rpm");
        checkProcessInfo.ArgumentList.Add("--device");
        checkProcessInfo.ArgumentList.Add(_config.DevicePath);
        checkProcessInfo.ArgumentList.Add("--drive");
        checkProcessInfo.ArgumentList.Add(_config.Drive);
        var checkProcess = System.Diagnostics.Process.Start(checkProcessInfo) ?? throw new Exception("Failed to start process");
        checkProcess.OutputDataReceived += logCallback;
        checkProcess.ErrorDataReceived += logCallback;
        checkProcess.BeginErrorReadLine();
        checkProcess.BeginOutputReadLine();

        await checkProcess.WaitForExitAsync(cancellationToken);
        if (!checkProcess.HasExited || checkProcess.ExitCode != 0)
        {
            process.Status = ProcessStatus.Failed;
            process.StatusDetail = StatusDetail.DriveCheckError.ToString();
            return null;
        }
        process.Status = ProcessStatus.Running;

        await logWriter.WriteLineAsync("Starting the digitalization process...");
        var outputFileName = Path.Combine(process.WorkDir, "output.scp");
        var gwProcessInfo = new ProcessStartInfo() {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            FileName = _config.GWPath,
        };
        gwProcessInfo.ArgumentList.Add("read");
        gwProcessInfo.ArgumentList.Add("--device");
        gwProcessInfo.ArgumentList.Add(_config.DevicePath);
        gwProcessInfo.ArgumentList.Add("--drive");
        gwProcessInfo.ArgumentList.Add(_config.Drive);
        gwProcessInfo.ArgumentList.Add(outputFileName);
        var gwProcess = System.Diagnostics.Process.Start(gwProcessInfo) ?? throw new Exception("Failed to start process");
        gwProcess.OutputDataReceived += logCallback;
        gwProcess.ErrorDataReceived += logCallback;
        gwProcess.BeginErrorReadLine();
        gwProcess.BeginOutputReadLine();

        await gwProcess.WaitForExitAsync(cancellationToken);
        if (!gwProcess.HasExited || gwProcess.ExitCode != 0)
        {
            // TODO: check other failure modes ("Command Failed: GetInfo: Bad Command")
            process.Status = ProcessStatus.Failed;
            process.StatusDetail = StatusDetail.FailedToProcessMedia.ToString();
            return null;
        }
        process.Status = ProcessStatus.Success;

        return new(outputFileName, ArtefactType.SfmFloppy);
    }

    private enum StatusDetail
    {
        FailedToProcessMedia,
        DriveCheckError
    }
}