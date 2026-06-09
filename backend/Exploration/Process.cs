using asec.Compatibility.EaasApi;
using asec.DataConversion;
using asec.Emulation;
using asec.LongRunning;
using asec.Models.Digitalization;
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
    private readonly EmulationConfig _explorationProcessConfig;
    private readonly Guid _explorationEnvironmentId;
    private readonly List<Artefact> _artefacts;
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
    protected Channel<ExplorationMessage> _inputChannel = Channel.CreateBounded<ExplorationMessage>(new BoundedChannelOptions(4)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false,
    });

    private Emulation.ExplorationProcess _prepEmuSubProcess = null;
    private Emulation.KioskProcess _kioskEmuSubProcess = null;
    private DataConversion.Process _conversionProcess = null;

    private string _playableEaasImageId;
    private string _prepEaasImageId;
    private string _prepEaasOuptutImageId;
    private string _prepSnapshotId;

    public Process(IConfiguration configuration, IServiceScopeFactory serviceProvider, Guid explorationEnvironmentId, List<Artefact> artefacts)
    {
        _serviceProvider = serviceProvider;

        _configuration = configuration;
        var section = configuration.GetSection("Emulation");

        _explorationProcessConfig = new EmulationConfig()
        {
            DirsBase =  section.GetValue<string>("ProcessBaseDir"),
            FfmpegPath = section.GetValue<string>("FfmpegPath"),
            MainDisplay = section.GetValue<string>("MainDisplay"),
            StreamBaseUrl = section.GetValue<string>("StreamOutBaseUrl"),
            WebcamDevice = section.GetValue<string>("WebcamDevice"),
            EaasTargetInputDrive = section.GetValue<string>("EaasDiskInputDrive"),
            EaasTargetOutputDrive = section.GetValue<string>("EaasDiskOutputDrive")
        };

        _explorationEnvironmentId = explorationEnvironmentId;
        _artefacts = artefacts;

        section = configuration.GetSection("Exploration");
        var baseDir = section.GetValue<string>("ProcessBaseDir")!;
        _processDir = Path.Combine(baseDir, Id.ToString());
    }

    public async Task Cleanup(CancellationToken cancellationToken = default)
    {
    }

    public async Task<ExplorationResult> Start(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_processDir);

        {
            var scope = _serviceProvider.CreateScope();
            StatusDetail.State = ExplorationState.InitialConversion;
            var conversionProcessManager = scope.ServiceProvider.GetRequiredService<IProcessManager<DataConversion.Process, ConversionResult, ConversionProcessDetail>>();
            var conversionProcess = new DataConversion.Process(
                new DataConversion.Converters.CopyConverter(null), // HACK: <- this should be handled better, maybe find an already constructed converter
                _artefacts,
                _serviceProvider,
                _configuration,
                Guid.Empty,
                Guid.Empty
            );
            var conversionResult = await conversionProcessManager.StartProcessAsync(conversionProcess);

            if (conversionProcess.Status != ProcessStatus.Success)
            {
                Status = ProcessStatus.Failed;
                return null;
            }

            StatusDetail.State = ExplorationState.InitialUpload;
            var eaasUploadClient = scope.ServiceProvider.GetRequiredService<EaasUploadClient>();
            var eaasEnvironmentRepoClient = scope.ServiceProvider.GetRequiredService<EnvironmentRepositoryClient>();
            var conversionBaseDir = _configuration.GetSection("").GetValue<string>("");

            // TODO: better image name
            _prepEaasImageId = await new ResultUploader(eaasUploadClient, null, eaasEnvironmentRepoClient, conversionBaseDir, null)
                .UploadImageToEaaS(conversionResult.Files, $"exploration[{Id}]", cancellationToken);

            var emptyImage = await Linux.MakeQcow2Image(1024 * 1024 * 1024, Path.Combine(_processDir, "output.qcow2"), FileSystem.Ext4, cancellationToken);
            _prepEaasOuptutImageId = await new ResultUploader(eaasUploadClient, null, eaasEnvironmentRepoClient, null, null)
                .UploadImageToEaaS(emptyImage, $"output[{Id}]", cancellationToken);
        }
        {
            var scope = _serviceProvider.CreateScope();
            StatusDetail.State = ExplorationState.ExplorationEnvironmentRunning;

            var emulationProcessManager = scope.ServiceProvider.GetRequiredService<IProcessManager<BaseProcess, EmulationResult, EmulationProcessDetail>>();
            var emulationProcess = new ExplorationProcess(_explorationEnvironmentId, _prepEaasImageId, _prepEaasOuptutImageId, _serviceProvider, _explorationProcessConfig, true);

            emulationProcessManager.StartProcess(emulationProcess);

            bool keepRunning = true;
            while (_inputChannel.Reader.TryRead(out _)); // NOTE: clear the input channel first

            while (keepRunning && !cancellationToken.IsCancellationRequested)
            {
                var message = await _inputChannel.Reader.ReadAsync(cancellationToken);

                switch (message)
                {
                    case ExplorationMessage.Ping:
                        await emulationProcess.ChannelWriter.WriteAsync(BaseProcess.EmulationMessage.Ping);
                        break;
                    case ExplorationMessage.GotoKiosk:
                        await emulationProcess.ChannelWriter.WriteAsync(BaseProcess.EmulationMessage.SaveMachineState);
                        await emulationProcess.ChannelWriter.WriteAsync(BaseProcess.EmulationMessage.Quit);
                        keepRunning = false;
                        break;
                }
            }

            var emulationResult = await emulationProcessManager.FinishProcessAsync(emulationProcess.Id);
            _prepSnapshotId = emulationResult.SnapshotId;
        }
        {
            StatusDetail.State = ExplorationState.TransferringKioskData;
            var scope = _serviceProvider.CreateScope();

            var eaasEnvRepoClient = scope.ServiceProvider.GetRequiredService<EnvironmentRepositoryClient>();
            var snapshotDetails = await eaasEnvRepoClient.GetEnvironmentDetails(_prepSnapshotId);

            var imageId = snapshotDetails.drives[int.Parse(_explorationProcessConfig.EaasTargetOutputDrive)].data;
            var snapshotData = await eaasEnvRepoClient.DownloadImage(imageId, _processDir);

            var imageInfo = await Linux.ReadImageInfo(snapshotData);
            while (imageInfo.RootElement.TryGetProperty("backing-filename", out var backingImgId))
            {
                var nextImage = await eaasEnvRepoClient.DownloadImage(backingImgId.GetString(), _processDir);
                imageInfo = await Linux.ReadImageInfo(nextImage);
            }
            var playableImage = await Linux.FlattenQcow2Image(snapshotData, Path.Combine(_processDir, "playable.qcow2"));

            var eaasUploadClient = scope.ServiceProvider.GetRequiredService<EaasUploadClient>();
            _playableEaasImageId = await new ResultUploader(eaasUploadClient, null, eaasEnvRepoClient, null, null).UploadImageToEaaS(playableImage, $"playable[{Id}]");
        }
        {
            StatusDetail.State = ExplorationState.KioskEnvironmentRunning;
            var scope = _serviceProvider.CreateScope();

            var emulationProcessManager = scope.ServiceProvider.GetRequiredService<IProcessManager<BaseProcess, EmulationResult, EmulationProcessDetail>>();
            // TODO: use a proper env id from exported json
            var emulationProcess = new ExplorationProcess(_explorationEnvironmentId, _playableEaasImageId, null, _serviceProvider, _explorationProcessConfig, true);
            emulationProcessManager.StartProcess(emulationProcess);

            bool keepRunning = true;
            while (_inputChannel.Reader.TryRead(out _)); // NOTE: clear the input channel first

            while (keepRunning && !cancellationToken.IsCancellationRequested)
            {
                var message = await _inputChannel.Reader.ReadAsync(cancellationToken);

                switch (message)
                {
                    case ExplorationMessage.Ping:
                        await emulationProcess.ChannelWriter.WriteAsync(BaseProcess.EmulationMessage.Ping);
                        break;
                    case ExplorationMessage.GotoExploration:
                        await emulationProcess.ChannelWriter.WriteAsync(BaseProcess.EmulationMessage.Quit);
                        keepRunning = false;
                        break;
                }
            }
        }

        return null;
    }

    public enum ExplorationMessage
    {
        Ping,
        GotoExploration,
        GotoKiosk,
        Quit
    }

    public enum ExplorationState
    {
        InitialConversion,
        InitialUpload,
        ExplorationEnvironmentRunning,
        TransferringKioskData,
        KioskEnvironmentRunning,
    }
}
