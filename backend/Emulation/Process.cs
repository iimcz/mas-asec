using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using asec.Compatibility.EaasApi;
using asec.Compatibility.EaasApi.ControlUrls;
using asec.Compatibility.EaasApi.Models;
using asec.LongRunning;
using asec.Models;
using asec.Models.Emulation;
using asec.Platforms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace asec.Emulation;

public class EmulationConfig
{
    public string DirsBase;
    public string FfmpegPath;
    public string MainDisplay;
    public string WebcamDevice;
    public string EaasTargetDrive;
    public string StreamBaseUrl;
}


public class Process : IProcess<EmulationResult>
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public CancellationToken CancellationToken { get; private set; }

    public DateTime StartTime { get; private set; }

    public string BaseDir { get; private set; }
    public string SubprocessLogsDir { get; private set; }
    public string RecordingsDir { get; private set; }

    public string LogPath { get; private set; }

    public ProcessStatus Status { get; private set; } = ProcessStatus.Initialization;

    public string StatusDetail { get; private set; }

    public ChannelWriter<EmulationMessage> ChannelWriter => _inputChannel.Writer;
    private Channel<EmulationMessage> _inputChannel = Channel.CreateBounded<EmulationMessage>(new BoundedChannelOptions(4)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false
    });

    public Guid PackageId { get; private set; }

    // TODO: have this variable for different emulators
    public bool IsGpuPassthrough => true;

    public bool IsUsbPassthrough { get; private set; }

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly EmulationConfig _config;

    public Process(Guid packageId, IServiceScopeFactory serviceScopeFactory, EmulationConfig config)
    {
        PackageId = packageId;
        _serviceScopeFactory = serviceScopeFactory;
        _config = config;

        BaseDir = Path.Combine(_config.DirsBase, Id.ToString());
        LogPath = Path.Combine(BaseDir, "log.txt");
        SubprocessLogsDir = Path.Combine(BaseDir, "sublogs");
        RecordingsDir = Path.Combine(BaseDir, "recordings");

        CreateDirectoryStructure();
    }

    private void CreateDirectoryStructure()
    {
        // Unnecessary in the current setup but for completeness
        Directory.CreateDirectory(BaseDir);

        Directory.CreateDirectory(SubprocessLogsDir);
        Directory.CreateDirectory(RecordingsDir);
        
        File.Create(LogPath).Close();
    }

    public async Task<EmulationResult> Start(CancellationToken cancellationToken)
    {
        using var logStream = new FileStream(LogPath, FileMode.Append);
        using var logWriter = new StreamWriter(logStream);
        logWriter.AutoFlush = true;
        CancellationToken = cancellationToken;
        StartTime = DateTime.Now;

        // TODO: do we need to keep the scope always? Explore other options.
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsecDBContext>();

        logWriter.WriteLine($"Looking up package: {PackageId}");
        var package = await dbContext.DigitalObjects
            .OfType<GamePackage>()
            .Include(p => p.Environment)
            .FirstOrDefaultAsync(p => p.Id == PackageId, cancellationToken);
        if (package == null)
        {
            Status = ProcessStatus.Failed;
            StatusDetail = EmulationStatusDetail.PackageNotFound.ToString();
            return new EmulationResult(null, String.Empty);
        }

        logWriter.WriteLine($"Starting EaaS package ID: {package.Environment.EaasId}");
        var componentsClient = scope.ServiceProvider.GetRequiredService<ComponentsClient>();
        var runningComponent = await componentsClient.StartComponent(new MachineComponentRequest(
            package.Environment.EaasId,
            new List<Drive>() {
                new Drive(_config.EaasTargetDrive, new ObjectDataSource(package.Id.ToString()))
            }
            ));
        var cachedState = await componentsClient.GetComponentState(runningComponent.id);
        Status = ProcessStatus.Running;

        var subTasks = new List<Task>() {
            Task.Run(() => AutoPassthroughUSB(runningComponent.id, Path.Combine(SubprocessLogsDir, "usbpassthrough.txt"), _config.MainDisplay), cancellationToken),
            Task.Run(() => PassthroughAndRecordScreen(Path.Combine(SubprocessLogsDir, "ffmpeg-screen.txt"), Path.Combine(RecordingsDir, "screen"), ".mp4"), cancellationToken),
            Task.Run(() => RecordWebcam(Path.Combine(SubprocessLogsDir, "ffmpeg-webcam.txt"), Path.Combine(RecordingsDir, "webcam.mp4"), _config.WebcamDevice), cancellationToken)
        };

        bool saveMachineState = true;
        bool keepRunning = true;

        while (keepRunning && !cancellationToken.IsCancellationRequested)
        {
            var message = await _inputChannel.Reader.ReadAsync(cancellationToken);

            switch (message)
            {
                case EmulationMessage.Ping:
                    await componentsClient.Keepalive(runningComponent.id);
                    break;
                case EmulationMessage.Quit:
                    keepRunning = false;
                    break;
                case EmulationMessage.SaveMachineState:
                    saveMachineState = true;
                    break;
                case EmulationMessage.NoSaveMachineState:
                    saveMachineState = false;
                    break;
            }
        }

        if (saveMachineState)
        {
            // TODO: handle saving machine state
        }
        await componentsClient.StopComponent(runningComponent.id);
        Status = ProcessStatus.Success;

        await Task.WhenAll(subTasks);

        // TODO: merge video files - right now we just choose the biggest one, as that represents
        // the longest continuous video stream. It is however possible that the recording gets
        // interrupted and restarted for some reason (maybe resolution changes? etc.), so
        // we should handle that as well.
        var largestScreenRecording = Directory.EnumerateFiles(RecordingsDir)
            .Where(f => Path.GetFileName(f).StartsWith("screen")).MaxBy(f => new FileInfo(f).Length);

        List<VideoFile> recordings = new();
        if (largestScreenRecording != null)
            recordings.Add(new(largestScreenRecording, RecordingType.Screen));
        
        // Add webcam recording if it exists.
        var webcamRecordingPath = Path.Combine(RecordingsDir, "webcam.mp4");
        if (File.Exists(webcamRecordingPath))
            recordings.Add(new(webcamRecordingPath, RecordingType.Webcam));

        // TODO: return actual values
        return new EmulationResult(
            recordings, ""
        );
    }

    private async Task RecordWebcam(string logPath, string recordingFile, string inputDevice)
    {
        using var logWriter = new StreamWriter(logPath);
        void outputCallback(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
                logWriter.WriteLine(e.Data);
        };

        ProcessStartInfo ffmpegInfo = new(_config.FfmpegPath);
        ffmpegInfo.ArgumentList.Add("-i");
        ffmpegInfo.ArgumentList.Add(inputDevice);

        // Recording config
        ffmpegInfo.ArgumentList.Add("-c:v");
        ffmpegInfo.ArgumentList.Add("h264");
        ffmpegInfo.ArgumentList.Add("-y");
        ffmpegInfo.ArgumentList.Add(recordingFile);

        ffmpegInfo.RedirectStandardOutput = true;
        ffmpegInfo.RedirectStandardError = true;

        var ffmpegProcess = System.Diagnostics.Process.Start(ffmpegInfo);
        if (ffmpegProcess == null)
            return; // TODO: error handling
        ffmpegProcess.OutputDataReceived += outputCallback;
        ffmpegProcess.ErrorDataReceived += outputCallback;
        ffmpegProcess.BeginOutputReadLine();
        ffmpegProcess.BeginErrorReadLine();

        while (Status == ProcessStatus.Running)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
        ffmpegProcess.Kill(Signum.SIGINT);
        await ffmpegProcess.WaitForExitAsync();
    }

    private async Task PassthroughAndRecordScreen(string logPath, string recordingPath, string recordingExt)
    {
        using var logWriter = new StreamWriter(logPath);
        void outputCallback(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
                logWriter.WriteLine(e.Data);
        };

        int ffmpegRetry = 0;
        string outputUrl = _config.StreamBaseUrl + Id.ToString();

        ProcessStartInfo ffmpegInfo = new(_config.FfmpegPath);
        ffmpegInfo.ArgumentList.Add("-f");
        ffmpegInfo.ArgumentList.Add("decklink");
        ffmpegInfo.ArgumentList.Add("-i");
        ffmpegInfo.ArgumentList.Add("Intensity Pro 4K"); // TODO: support more recording hardware

        // Streaming config
        ffmpegInfo.ArgumentList.Add("-c:v");
        ffmpegInfo.ArgumentList.Add("vp8");
        ffmpegInfo.ArgumentList.Add("-f");
        ffmpegInfo.ArgumentList.Add("rtsp");
        ffmpegInfo.ArgumentList.Add(outputUrl);

        // Recording config
        ffmpegInfo.ArgumentList.Add("-c:v");
        ffmpegInfo.ArgumentList.Add("h264");
        ffmpegInfo.ArgumentList.Add("-y");
        ffmpegInfo.ArgumentList.Add(recordingPath + ffmpegRetry + recordingExt);

        ffmpegInfo.RedirectStandardOutput = true;
        ffmpegInfo.RedirectStandardError = true;

        var ffmpegProcess = System.Diagnostics.Process.Start(ffmpegInfo);
        if (ffmpegProcess == null)
            return; // TODO: error handling
        ffmpegProcess.OutputDataReceived += outputCallback;
        ffmpegProcess.ErrorDataReceived += outputCallback;
        ffmpegProcess.BeginOutputReadLine();
        ffmpegProcess.BeginErrorReadLine();

        while (Status == ProcessStatus.Running)
        {
            if (ffmpegProcess.HasExited)
            {
                // Keep restarting ffmpeg
                await logWriter.WriteLineAsync("## Restarting FFMPEG...");
                await logWriter.FlushAsync();
                ffmpegRetry++;
                await Task.Delay(TimeSpan.FromSeconds(2));
                ffmpegInfo.ArgumentList[^1] = recordingPath + ffmpegRetry + recordingExt;
                ffmpegProcess = System.Diagnostics.Process.Start(ffmpegInfo)!;
                ffmpegProcess.OutputDataReceived += outputCallback;
                ffmpegProcess.ErrorDataReceived += outputCallback;
                ffmpegProcess.BeginOutputReadLine();
                ffmpegProcess.BeginErrorReadLine();
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }
        if (!ffmpegProcess.HasExited)
        {
            ffmpegProcess.Kill(Signum.SIGINT);
        }

        await ffmpegProcess.WaitForExitAsync();
    }

    private async Task AutoPassthroughUSB(string componentId, string logPath, string displayToCheck)
    {
        using var logWriter = new StreamWriter(logPath);
        using var scope = _serviceScopeFactory.CreateScope();
        logWriter.AutoFlush = true;

        var componentsClient = scope.ServiceProvider.GetRequiredService<ComponentsClient>();
        var controlUrls = await componentsClient.GetControlUrls(componentId);
        var qemuClient = new QemuControlUrlClient(controlUrls["qemu"]);

        var devices = await qemuClient.GetDeviceInfos();

        bool lastConnected = true;

        while (Status == ProcessStatus.Running)
        {
            bool connected = await Linux.PollDisplayConnected(displayToCheck);
            if (connected != lastConnected)
            {
                await logWriter.WriteLineAsync("Display connection status changed, running connection commands:");
                foreach (var device in devices)
                {
                    if (device.deviceType == "keyboard" || device.deviceType == "mouse")
                    {
                        // Disconnect device when we switch back to the integrated GPU
                        // -> connected is true
                        var command = connected ? device.disconnectCommand : device.connectCommand;
                        await logWriter.WriteLineAsync($"\t{command}");
                        await qemuClient.PostCommand(command);
                    }
                }
                lastConnected = connected;
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    // TODO: There might be a better way to handle the finish request
    // other than just sending four messages to set up the right situation in sequence.
    public enum EmulationMessage
    {
        Ping,
        SaveMachineState,
        NoSaveMachineState,
        Quit
    }

    private enum EmulationStatusDetail
    {
        PackageNotFound
    }
}
