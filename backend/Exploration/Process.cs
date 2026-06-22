using asec.Compatibility.EaasApi;
using asec.DataConversion;
using asec.Emulation;
using asec.LongRunning;
using asec.Models;
using asec.Models.Archive;
using asec.Models.Digitalization;
using asec.Models.Emulation;
using asec.Platforms;
using System.Threading.Channels;

namespace asec.Exploration;

public class ExplorationProcessDetail
{
    public Process.ExplorationState State { get; set; }
}

public class Process : IProcess<ExplorationResult, ExplorationProcessDetail>
{
    private readonly IServiceScopeFactory _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly string _emulationStreamBaseUrl;
    private readonly EmulationConfig _emulationProcessConfig;
    private readonly Guid _explorationEnvironmentId;
    private readonly List<Artefact> _artefacts;
    private readonly WorkVersion _version;
    private readonly string _processDir;

    public Guid Id { get; private set; } = Guid.NewGuid();

    public CancellationToken CancellationToken { get; private set; } = CancellationToken.None;

    public DateTime StartTime { get; private set; }

    public string BaseDir { get; private set; }
    public string LogPath { get; private set; }

    public ProcessStatus Status { get; private set; } = ProcessStatus.Initialization;
    public bool IsSubprocess => false;
    public ExplorationProcessDetail StatusDetail { get; private set; } = new();

    public ChannelWriter<ExplorationMessage> ChannelWriter => _inputChannel.Writer;

    public string CurrentStreamUrl { get; private set; } = "";

    public PlayableObject LatestPlayableObject { get; private set; } = null;

    private Guid _kioskEnvironmentId;

    protected Channel<ExplorationMessage> _inputChannel = Channel.CreateBounded<ExplorationMessage>(new BoundedChannelOptions(4)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false,
    });

    private string _playableEaasImageId;
    private string _prepEaasImageId;
    private string _prepEaasOuptutImageId;
    private string _prepSnapshotId;
    private string _playableImage;
    private string _outputImage;
    private ConversionResult _initialConversionResult;

    public Process(IConfiguration configuration, IServiceScopeFactory serviceProvider, Guid explorationEnvironmentId, List<Artefact> artefacts, WorkVersion version)
    {
        _serviceProvider = serviceProvider;

        _configuration = configuration;
        var section = configuration.GetSection("Emulation");

        _emulationStreamBaseUrl = section.GetValue<string>("StreamBaseUrl");

        _emulationProcessConfig = new EmulationConfig()
        {
            DirsBase = section.GetValue<string>("ProcessBaseDir"),
            FfmpegPath = section.GetValue<string>("FfmpegPath"),
            MainDisplay = section.GetValue<string>("MainDisplay"),
            StreamBaseUrl = section.GetValue<string>("StreamOutBaseUrl"),
            WebcamDevice = section.GetValue<string>("WebcamDevice"),
            EaasTargetInputDrive = section.GetValue<string>("EaasDiskInputDrive"),
            EaasTargetOutputDrive = section.GetValue<string>("EaasDiskOutputDrive")
        };

        _explorationEnvironmentId = explorationEnvironmentId;
        _artefacts = artefacts;
        _version = version;

        section = configuration.GetSection("Exploration");
        var baseDir = section.GetValue<string>("ProcessBaseDir")!;
        _processDir = Path.Combine(baseDir, Id.ToString());
    }

    public async Task Cleanup(CancellationToken cancellationToken = default)
    {
    }

    public async Task<ExplorationResult> Start(CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        StartTime = DateTime.Now;
        BaseDir = _processDir;
        LogPath = Path.Combine(_processDir, "log.txt");
        Directory.CreateDirectory(_processDir);

        Status = ProcessStatus.Running;
        StatusDetail.State = ExplorationState.InitialConversion;
        while (StatusDetail.State != ExplorationState.Done && !cancellationToken.IsCancellationRequested)
        {
            var nextState = StatusDetail.State switch
            {
                ExplorationState.InitialConversion => await PerformInitialConversion(),
                ExplorationState.PrepareOutputImage => await PrepareOutputImage(),
                ExplorationState.InitialUpload => await PerformInitialUpload(),
                ExplorationState.ExplorationEnvironmentRunning => await RunExplorationEnvironment(),
                ExplorationState.DownloadExplorationData => await DownloadExplorationData(),
                ExplorationState.ExtractingPlayableInfo => await ExtractPlayableInfo(),
                ExplorationState.WaitingForCheck => await WaitForCheck(),
                ExplorationState.UploadKioskData => await UploadKioskData(),
                ExplorationState.KioskEnvironmentRunning => await RunKioskEnvironment(),
                ExplorationState.Done => ExplorationState.Done,
                ExplorationState.Aborted => ExplorationState.Aborted,
                _ => throw new NotImplementedException(),
            };

            if (StatusDetail.State == ExplorationState.Aborted)
            {
                Status = ProcessStatus.Failed;
                break;
            }

            if (StatusDetail.State == ExplorationState.Done)
            {
                Status = ProcessStatus.Success;
                break;
            }

            StatusDetail.State = nextState;
        }

        if (StatusDetail.State == ExplorationState.Done)
        {
            return new() {
                IncludedArtefactIds = _artefacts.Select(a => a.Id).ToList(),
                VersionId = _version.Id,
                PlayableObject = LatestPlayableObject
            };
        }
        return null;
    }

    private async Task<ExplorationState> PerformInitialConversion()
    {
        using var scope = _serviceProvider.CreateScope();

        var conversionProcessManager = scope.ServiceProvider.GetRequiredService<IProcessManager<DataConversion.Process, ConversionResult, ConversionProcessDetail>>();
        var conversionProcess = new DataConversion.Process(
            new DataConversion.Converters.CopyConverter(null), // HACK: for now use just a hardcoded converter - this should use a deduced one later, though still likely just CopyConverter
            _artefacts,
            _serviceProvider,
            _configuration,
            Guid.Empty,
            Guid.Empty,
            true
        );
        _initialConversionResult = await conversionProcessManager.StartProcessAsync(conversionProcess);

        if (conversionProcess.Status != ProcessStatus.Success)
        {
            Status = ProcessStatus.Failed;
        }
        return ExplorationState.PrepareOutputImage;
    }

    private async Task<ExplorationState> PrepareOutputImage()
    {
        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Process>>();

        // TODO: choose a better size, using some variable, ideally...
        _outputImage = Path.Combine(_processDir, $"output[{Id}].qcow2");
        var outputImageMount = Path.Combine(_processDir, $"output[{Id}]_mounted");
        Directory.CreateDirectory(outputImageMount);

        var log = await Linux.MakeQcow2Image(1024 * 1024 * 1024, _outputImage, FileSystem.Ext4, "game");
        logger.LogInformation(log);

        log = await Linux.MountQcow2Image(_outputImage, outputImageMount);
        logger.LogInformation(log);

        var section = _configuration.GetSection("Exploration");
        var defaultEmuSlug = section.GetValue<string>("DefaultEmulatorSlug");
        var defaultJson = new PlayableObjectDef() {
            EmulatorSlug = defaultEmuSlug
        };

        using (FileStream fs = new(Path.Combine(outputImageMount, "game.json"), FileMode.CreateNew))
        {
            System.Text.Json.JsonSerializer.Serialize(fs, defaultJson);
        }

        log = await Linux.UnmountQcow2Image(outputImageMount);
        logger.LogInformation(log);

        return ExplorationState.InitialUpload;
    }

    private async Task<ExplorationState> PerformInitialUpload()
    {
        using var scope = _serviceProvider.CreateScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Process>>();

        var eaasUploadClient = scope.ServiceProvider.GetRequiredService<EaasUploadClient>();
        var eaasEnvironmentRepoClient = scope.ServiceProvider.GetRequiredService<EnvironmentRepositoryClient>();
        var conversionBaseDir = Path.Combine(BaseDir, "subprocesses", "conversion");
        Directory.CreateDirectory(conversionBaseDir);

        _prepEaasImageId = await new ResultUploader(eaasUploadClient, null, eaasEnvironmentRepoClient, conversionBaseDir, logger)
            .UploadImageToEaaS(_initialConversionResult.Files, $"exploration[{Id}]");

        _prepEaasOuptutImageId = await new ResultUploader(eaasUploadClient, null, eaasEnvironmentRepoClient, null, logger)
            .UploadImageToEaaS(_outputImage, $"output[{Id}]");

        return ExplorationState.ExplorationEnvironmentRunning;
    }

    private async Task<ExplorationState> RunExplorationEnvironment()
    {
        using var scope = _serviceProvider.CreateScope();
        StatusDetail.State = ExplorationState.ExplorationEnvironmentRunning;
        LatestPlayableObject = null;

        var emulationProcessManager = scope.ServiceProvider.GetRequiredService<IProcessManager<BaseProcess, EmulationResult, EmulationProcessDetail>>();
        var emulationProcess = new ExplorationProcess(_explorationEnvironmentId, _prepEaasImageId, _prepEaasOuptutImageId, _serviceProvider, _emulationProcessConfig, true);

        emulationProcessManager.StartProcess(emulationProcess);
        CurrentStreamUrl =_emulationStreamBaseUrl + emulationProcess.Id.ToString();

        while (_inputChannel.Reader.TryRead(out _)) ;

        while (!CancellationToken.IsCancellationRequested)
        {
            var message = await _inputChannel.Reader.ReadAsync(CancellationToken);

            switch (message)
            {
                case ExplorationMessage.Ping:
                    await emulationProcess.ChannelWriter.WriteAsync(BaseProcess.EmulationMessage.Ping);
                    break;
                case ExplorationMessage.GotoCheck:
                    {
                        await emulationProcess.ChannelWriter.WriteAsync(BaseProcess.EmulationMessage.SaveMachineState);
                        await emulationProcess.ChannelWriter.WriteAsync(BaseProcess.EmulationMessage.Quit);
                        var emulationResult = await emulationProcessManager.FinishProcessAsync(emulationProcess.Id);
                        _prepSnapshotId = emulationResult.SnapshotId;
                        return ExplorationState.DownloadExplorationData;
                    }
                case ExplorationMessage.Abort:
                    return ExplorationState.Aborted;
            }
        }

        return ExplorationState.Aborted;
    }

    private async Task<ExplorationState> DownloadExplorationData()
    {
        using var scope = _serviceProvider.CreateScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Process>>();
        logger.LogInformation($"Downloading data from snapshot {_prepSnapshotId}");

        var eaasEnvRepoClient = scope.ServiceProvider.GetRequiredService<EnvironmentRepositoryClient>();
        var snapshotDetails = await eaasEnvRepoClient.GetEnvironmentDetails(_prepSnapshotId);

        var imageId = snapshotDetails.drives[int.Parse(_emulationProcessConfig.EaasTargetOutputDrive)].data;
        imageId = imageId.Substring("binding://".Length);

        logger.LogInformation($"Downloading image {imageId}");
        if (File.Exists(Path.Combine(_processDir, imageId))) File.Delete(Path.Combine(_processDir, imageId));
        var snapshotData = await eaasEnvRepoClient.DownloadImage(imageId, _processDir);

        logger.LogInformation($"Top-level image downladed to {snapshotData}");

        var imageInfo = await Linux.ReadImageInfo(snapshotData);
        while (imageInfo.RootElement.TryGetProperty("backing-filename", out var backingImgId))
        {
            if (File.Exists(Path.Combine(_processDir, backingImgId.GetString()!))) File.Delete(Path.Combine(_processDir, backingImgId.GetString()!));
            var nextImage = await eaasEnvRepoClient.DownloadImage(backingImgId.GetString(), _processDir);
            imageInfo = await Linux.ReadImageInfo(nextImage);
        }
        _playableImage = Path.Combine(_processDir, "playable.qcow2");
        if (File.Exists(_playableImage)) File.Delete(_playableImage);
        var flattenOutput = await Linux.FlattenQcow2Image(snapshotData, _playableImage);
        logger.LogInformation(flattenOutput);

        return ExplorationState.ExtractingPlayableInfo;
    }

    private async Task<ExplorationState> ExtractPlayableInfo()
    {
        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Process>>();

        var playableMountpoint = Path.Combine(_processDir, "playable_mounted");
        Directory.CreateDirectory(playableMountpoint);

        var output = await Linux.MountQcow2Image(_playableImage, playableMountpoint);
        logger.LogInformation(output);

        var jsonPath = Path.Combine(playableMountpoint, "game.json");
        if (Path.Exists(jsonPath))
        {
            using FileStream fs = new(jsonPath, FileMode.Open);
            var jsonData = System.Text.Json.JsonSerializer.Deserialize<PlayableObjectDef>(fs);

            if (jsonData != null)
            {
                using AsecDBContext dbContext = scope.ServiceProvider.GetRequiredService<AsecDBContext>();
                var environment = dbContext.Environments.Where(e => e.Slug == jsonData.EmulatorSlug).FirstOrDefault();

                _kioskEnvironmentId = environment?.Id ?? Guid.Empty;

                LatestPlayableObject = new() {
                    Id = Guid.Empty,
                    Label = jsonData.Label,
                    InternalNote = jsonData.Note,
                    CreationDate = DateTime.Now,
                    Environment = environment,
                    IncludedDigitalObjects = _artefacts.Select(a => (DigitalObject)a).ToList()
                };
            }
            else
            {
                LatestPlayableObject = null;
            }
        }
        else
        {
            LatestPlayableObject = null;
        }

        output = await Linux.UnmountQcow2Image(playableMountpoint);
        logger.LogInformation(output);

        return ExplorationState.WaitingForCheck;
    }

    private async Task<ExplorationState> WaitForCheck()
    {
        while (!CancellationToken.IsCancellationRequested)
        {
            var message = await _inputChannel.Reader.ReadAsync(CancellationToken);

            switch (message)
            {
                case ExplorationMessage.GotoExploration:
                    return ExplorationState.ExplorationEnvironmentRunning;
                case ExplorationMessage.GotoKiosk:
                    return ExplorationState.UploadKioskData;
            }
        }

        return ExplorationState.Aborted;
    }


    private async Task<ExplorationState> UploadKioskData()
    {
        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Process>>();

        var eaasUploadClient = scope.ServiceProvider.GetRequiredService<EaasUploadClient>();
        var eaasEnvRepoClient = scope.ServiceProvider.GetRequiredService<EnvironmentRepositoryClient>();

        _playableEaasImageId = await new ResultUploader(eaasUploadClient, null, eaasEnvRepoClient, null, logger).UploadImageToEaaS(_playableImage, $"playable[{Id}]");

        return ExplorationState.KioskEnvironmentRunning;
    }

    private async Task<ExplorationState> RunKioskEnvironment()
    {
        StatusDetail.State = ExplorationState.KioskEnvironmentRunning;

        using var scope = _serviceProvider.CreateScope();

        var emulationProcessManager = scope.ServiceProvider.GetRequiredService<IProcessManager<BaseProcess, EmulationResult, EmulationProcessDetail>>();
        var emulationProcess = new KioskCheckProcess(_kioskEnvironmentId, _playableEaasImageId, _serviceProvider, _emulationProcessConfig, true);

        emulationProcessManager.StartProcess(emulationProcess);
        CurrentStreamUrl = _emulationStreamBaseUrl + emulationProcess.Id.ToString();

        while (_inputChannel.Reader.TryRead(out _)) ;

        while (!CancellationToken.IsCancellationRequested)
        {
            var message = await _inputChannel.Reader.ReadAsync(CancellationToken);

            ExplorationState? nextState = null;

            switch (message)
            {
                case ExplorationMessage.Ping:
                    await emulationProcess.ChannelWriter.WriteAsync(BaseProcess.EmulationMessage.Ping);
                    break;
                case ExplorationMessage.GotoExploration:
                    nextState = ExplorationState.ExplorationEnvironmentRunning;
                    break;
                case ExplorationMessage.GotoCheck:
                    nextState = ExplorationState.ExtractingPlayableInfo;
                    break;
                case ExplorationMessage.Abort:
                    nextState = ExplorationState.Aborted;
                    break;
                case ExplorationMessage.Done:
                    nextState = ExplorationState.Done;
                    break;
            }

            if (nextState != null)
            {
                await emulationProcess.ChannelWriter.WriteAsync(BaseProcess.EmulationMessage.SaveMachineState);
                await emulationProcess.ChannelWriter.WriteAsync(BaseProcess.EmulationMessage.Quit);
                await emulationProcessManager.FinishProcessAsync(emulationProcess.Id);
                return (ExplorationState) nextState;
            }
        }

        return ExplorationState.Aborted;
    }

    public enum ExplorationMessage
    {
        Ping,
        GotoExploration,
        GotoCheck,
        GotoKiosk,
        Save,
        Abort,
        Done
    }

    public enum ExplorationState
    {
        InitialConversion,
        PrepareOutputImage,
        InitialUpload,
        ExplorationEnvironmentRunning,
        DownloadExplorationData,
        ExtractingPlayableInfo,
        WaitingForCheck,
        UploadKioskData,
        KioskEnvironmentRunning,
        Aborted,
        Done
    }
}
