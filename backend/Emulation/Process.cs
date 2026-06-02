using System.Diagnostics;
using System.Threading.Channels;
using asec.Compatibility.EaasApi;
using asec.Compatibility.EaasApi.ControlUrls;
using asec.Compatibility.EaasApi.Models;
using asec.LongRunning;
using asec.Models;
using asec.Models.Emulation;
using asec.Platforms;
using Microsoft.EntityFrameworkCore;

namespace asec.Emulation;

public class EmulationConfig
{
    public string DirsBase;
    public string FfmpegPath;
    public string MainDisplay;
    public string WebcamDevice;
    public string EaasTargetInputDrive;
    public string EaasTargetOutputDrive;
    public string StreamBaseUrl;
}

public class EmulationProcessDetail
{
    public bool IsGpuPassthrough { get; set; }
    public bool IsUsbPassthrough { get; set; }
    public string Other { get; set; }
}

public abstract class BaseProcess : IProcess<EmulationResult, EmulationProcessDetail>
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public CancellationToken CancellationToken { get; private set; }

    public DateTime StartTime { get; private set; }

    public string BaseDir { get; private set; }
    public string SubprocessLogsDir { get; private set; }
    public string RecordingsDir { get; private set; }

    public string LogPath { get; private set; }

    public ProcessStatus Status { get; private set; } = ProcessStatus.Initialization;
    public EmulationProcessDetail StatusDetail { get; private set; } = new()
    {
        IsGpuPassthrough = true,
        IsUsbPassthrough = true,
        Other = ""
    };

    public ChannelWriter<EmulationMessage> ChannelWriter => _inputChannel.Writer;
    protected Channel<EmulationMessage> _inputChannel = Channel.CreateBounded<EmulationMessage>(new BoundedChannelOptions(4)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false
    });

    protected readonly IServiceScopeFactory _serviceScopeFactory;
    protected readonly EmulationConfig _config;
    protected StreamWriter _logWriter;

    public BaseProcess(IServiceScopeFactory serviceScopeFactory, EmulationConfig config)
    {
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


        // TODO: maybe find a better way? This might be dangerous regarding resource disposal...
        _logWriter = logWriter;

        // TODO: do we need to keep the scope always? Explore other options.

        var environment = await ResolveEnvironment();
        if (environment == null)
        {
            Status = ProcessStatus.Failed;
            StatusDetail.Other = EmulationStatusDetail.PackageNotFound.ToString(); // TODO: make this environment not found
            return new EmulationResult(null, String.Empty);
        }

        List<Drive> envDrives = new();
        var inputImage = ResolveInputImageId();
        if (inputImage != null)
            envDrives.Add(new(_config.EaasTargetInputDrive, new ImageDataSource(inputImage)));
        var outputImage = ResolveOutputImageId();
        if (outputImage != null)
            envDrives.Add(new(_config.EaasTargetOutputDrive, new ImageDataSource(outputImage)));

        List<Func<Task>> keepAlives = [];

        using var scope = _serviceScopeFactory.CreateScope();

        logWriter.WriteLine($"Starting EaaS environment ID: {environment.EaasId}");
        var componentsClient = scope.ServiceProvider.GetRequiredService<ComponentsClient>();
        var runningComponent = await componentsClient.StartComponent(new MachineComponentRequest(environment.EaasId, envDrives));
        var cachedState = await componentsClient.GetComponentState(runningComponent.id);
        keepAlives.Add(async () => await componentsClient.Keepalive(runningComponent.id));

        if (environment.InternetConnected)
        {
            logWriter.WriteLine($"Emulator environment wants internet, starting network for component ID: {runningComponent.id}");
            var networkClient = scope.ServiceProvider.GetRequiredService<NetworksClient>();
            // TODO: specify these in a better way to avoid the wave of nulls
            var networkResponse = await networkClient.StartNetwork(
                new(
                    [
                        new(
                            runningComponent.id,
                            "machine 0",
                            null, null, null,
                            "auto",
                            true
                        )
                    ],
                    true,
                    true,
                    false,
                    null,
                    null,
                    null,
                    null
                )
            );
            if (networkResponse.id == null || networkResponse.id.Length <= 0)
            {
                logWriter.WriteLine("Failed to start network, ending emulation.");
                Status = ProcessStatus.Failed;
                return new([], null);
            }

            var sessionsClient = scope.ServiceProvider.GetRequiredService<SessionsClient>();
            keepAlives.Add(async () => await sessionsClient.Keepalive(networkResponse.id));
        }

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
                    await Task.WhenAll(keepAlives.Select(f => f()));
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

        string snapshotId = null;
        if (saveMachineState)
        {
            snapshotId = await componentsClient.SnapshotComponent(runningComponent.id, environment.EaasId, $"Emulation snapshot at {DateTime.Now.ToString()}");
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
            recordings, snapshotId
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

    public Task Cleanup(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
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

    protected abstract Task<EmulationEnvironment> ResolveEnvironment(CancellationToken cancellationToken = default);
    protected abstract string ResolveInputImageId();
    protected abstract string ResolveOutputImageId();
}


public class PreparationProcess : BaseProcess
{
    public Guid EnvironmentId { get; private set; }
    public string DigitalObjectsImageId { get; private set; }

    public PreparationProcess(Guid environmentId, string digitalObjectsImageId, IServiceScopeFactory serviceScopeFactory, EmulationConfig config) : base(serviceScopeFactory, config)
    {
        EnvironmentId = environmentId;
        DigitalObjectsImageId = digitalObjectsImageId;
    }

    protected override async Task<EmulationEnvironment> ResolveEnvironment(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsecDBContext>();
        _logWriter.WriteLine($"Looking up exploration environment: {EnvironmentId}");
        var environment = await dbContext.Environments
            .Where(e => e.Type == EnvironmentType.Preparation)
            .FirstOrDefaultAsync(e => e.Id == EnvironmentId);
        return environment;
    }

    protected override string ResolveInputImageId()
    {
        // TODO: implement
        return null;
    }

    protected override string ResolveOutputImageId()
    {
        // TODO: implement
        return null;
    }

}

public class KioskProcess : BaseProcess
{
    public Guid PackageId { get; private set; }

    public KioskProcess(Guid packageId, IServiceScopeFactory serviceScopeFactory, EmulationConfig config) : base(serviceScopeFactory, config)
    {
        PackageId = packageId;
    }

    protected override async Task<EmulationEnvironment> ResolveEnvironment(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsecDBContext>();
        _logWriter.WriteLine($"Looking up package: {PackageId}");
        var package = await dbContext.DigitalObjects
            .OfType<GamePackage>()
            .Include(p => p.Environment)
            .FirstOrDefaultAsync(p => p.Id == PackageId, cancellationToken);
        return package?.Environment;
    }

    protected override string ResolveInputImageId()
    {
        // TODO: implement
        return null;
    }

    protected override string ResolveOutputImageId() => null;
}
