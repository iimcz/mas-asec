

using System.Diagnostics;
using System.Text.RegularExpressions;
using asec.Models.Digitalization;
using asec.Platforms;

namespace asec.Digitalization.Tools;

public class FfmpegRecordingToolConfig : DigitalizationToolConfig
{
    public string FfmpegPath { get; set; }
    public string InputDevice { get; set; }
    public string InputPath { get; set; }
    public List<string> Arguments { get; set; } = new();

    public override IDigitalizationTool ConstructTool()
    {
        return new FfmpegRecordingTool(this);
    }
}

public class FfmpegRecordingTool : IDigitalizationTool
{
    private readonly Regex ToolVersionRegex = new Regex(@"^ffmpeg version (\d+).(\d+).(\d+)");
    private readonly Regex DeviceRegex = new Regex(@"^ ([D ])[E ]\s(\w+)");

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Slug => _config.Slug;

    public string Name => "Ffmpeg";

    public string Version { get; private set; } = "";

    public string Environment {
        get {
            string OS = System.Environment.OSVersion.VersionString;
            string CLR = System.Environment.Version.ToString();
            return $"OS:{OS},CLR:{CLR}";
        }
    }

    // TODO: consider using Ffmpeg for more media types, but which?
    public PhysicalMediaType PhysicalMedia => PhysicalMediaType.AudioCassette;

    public bool IsAvailable { get; private set; } = false;

    private FfmpegRecordingToolConfig _config;

    public FfmpegRecordingTool(FfmpegRecordingToolConfig config)
    {
        _config = config;
    }


    public async Task Initialize(CancellationToken cancellationToken)
    {
        if (Path.Exists(_config.FfmpegPath))
        {
            ProcessStartInfo ffProcessInfo = new ProcessStartInfo(_config.FfmpegPath)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            ffProcessInfo.ArgumentList.Add("-devices");
            var ffProcess = System.Diagnostics.Process.Start(ffProcessInfo);
            if (ffProcess == null)
                return;
            await ffProcess.WaitForExitAsync(cancellationToken);
            if (!ffProcess.HasExited || ffProcess.ExitCode != 0)
                return;

            string versionLine = ffProcess.StandardError.ReadLine() ?? "";
            var versionMatch = ToolVersionRegex.Match(versionLine);
            if (!versionMatch.Success)
                return;
            Version = $"{versionMatch.Groups[1]}.{versionMatch.Groups[2]}.{versionMatch.Groups[3]}";

            bool haveDevice = false;
            while (!ffProcess.StandardOutput.EndOfStream)
            {
                string line = ffProcess.StandardOutput.ReadLine() ?? "";
                var deviceMatch = DeviceRegex.Match(line);
                if (!deviceMatch.Success)
                    continue;
                
                if (deviceMatch.Groups[1].Value == "D" && deviceMatch.Groups[2].Value == _config.InputDevice)
                {
                    haveDevice = true;
                    break;
                }
            }

            if (haveDevice)
                IsAvailable = true;
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
        process.Status = LongRunning.ProcessStatus.Running;

        await logWriter.WriteLineAsync("Asking for cassette rewind.");
        await process.WaitForInput(StatusDetail.RequestForRewind.ToString(), cancellationToken);
        await logWriter.WriteLineAsync("Rewind (hopefully) done, starting recording.");

        string outputFile = Path.Combine(process.WorkDir, "cassette.wav");
        ProcessStartInfo ffProcessInfo = new ProcessStartInfo(_config.FfmpegPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        ffProcessInfo.ArgumentList.Add("-f");
        ffProcessInfo.ArgumentList.Add(_config.InputDevice);
        ffProcessInfo.ArgumentList.Add("-i");
        ffProcessInfo.ArgumentList.Add(_config.InputPath);
        ffProcessInfo.ArgumentList.Add(outputFile);
        _config.Arguments.ForEach(a => ffProcessInfo.ArgumentList.Add(a));
        var ffProcess = System.Diagnostics.Process.Start(ffProcessInfo) ?? throw new Exception("Failed to start process!");
        ffProcess.ErrorDataReceived += logCallback;
        ffProcess.OutputDataReceived += logCallback;
        ffProcess.BeginErrorReadLine();
        ffProcess.BeginOutputReadLine();
        await logWriter.WriteLineAsync("Recording started, requesting cassette play from user.");
        await process.WaitForInput(StatusDetail.RequestForPlay.ToString(), cancellationToken);

        await logWriter.WriteLineAsync("Now recording and requesting user input for when the cassette ends (or the program does) and we should stop recording.");
        await process.WaitForInput(StatusDetail.RequestForStopNotification.ToString(), cancellationToken);

        ffProcess.Kill(Signum.SIGINT);
        await ffProcess.WaitForExitAsync(cancellationToken);
        if (!ffProcess.HasExited)
            throw new Exception("Failed to exit!");

        return new(outputFile, ArtefactType.WavAudio);
    }

    private enum StatusDetail
    {
        RequestForRewind,
        RequestForPlay,
        RequestForStopNotification
    }
}