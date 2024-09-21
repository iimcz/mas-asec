using System.Diagnostics;
using System.Threading.Channels;
using asec.DataConversion.Converters;
using asec.LongRunning;
using asec.Models.Digitalization;
using Minio;
using Minio.DataModel.Args;

namespace asec.DataConversion;

public class Process : IProcess<ConversionResult>
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid EnvironmentId { get; private set; }

    public CancellationToken CancellationToken { get; private set; }

    public DateTime StartTime { get; private set; }

    public string BaseDir { get; private set; }
    public string FetchDir { get; private set; }
    public string WorkDir { get; private set; }
    public string OutputDir { get; private set; }

    public string LogPath { get; private set; }

    public ProcessStatus Status { get; set; }

    public string StatusDetail { get; set; }
    public List<Artefact> Artefacts { get; set; }
    public ChannelWriter<string> InputChannel => _inputChannel.Writer;
    private Channel<string> _inputChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(1) {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false
    });

    public IConverter Converter { get; private set; }
    private IServiceScopeFactory _serviceScopeFactory;

    // TODO: maybe replace with handlers for individual file types?
    private readonly string _unzipBinary;
    private readonly string _artefactBucket;

    public Process(Guid environmentId, IConverter converter, List<Artefact> artefacts, IServiceScopeFactory serviceScopeFactory, string dirsBase, string artefactBucket, string unzipBin)
    {
        Converter = converter;
        _serviceScopeFactory = serviceScopeFactory;
        _unzipBinary = unzipBin;
        _artefactBucket = artefactBucket;
        Artefacts = artefacts;
        EnvironmentId = environmentId;

        BaseDir = Path.Combine(dirsBase, Id.ToString());
        FetchDir = Path.Combine(BaseDir, "fetched");
        WorkDir = Path.Combine(BaseDir, "work");
        OutputDir = Path.Combine(BaseDir, "output");
        LogPath = Path.Combine(BaseDir, "log.txt");

        CreateDirectoryStructure();
    }

    public Task<ConversionResult> Start(CancellationToken cancellationToken)
    {
        StartTime = DateTime.Now;
        CancellationToken = cancellationToken;
        return Converter.Start(this, cancellationToken);
    }

    private void CreateDirectoryStructure()
    {
        // Unnecessary in the current setup but for completeness
        Directory.CreateDirectory(BaseDir);

        Directory.CreateDirectory(FetchDir);
        Directory.CreateDirectory(WorkDir);
        Directory.CreateDirectory(OutputDir);

        File.Create(LogPath).Close();
    }

    public async Task<string> FetchArtefact(Artefact artefact, bool extractArchive, CancellationToken cancellationToken = default)
    {
        string artefactFetchDir = Path.Combine(FetchDir, artefact.Id.ToString());
        Directory.CreateDirectory(artefactFetchDir);

        using (var serviceScope = _serviceScopeFactory.CreateScope())
        {
            var minioClient = serviceScope.ServiceProvider.GetRequiredService<IMinioClient>();
            var args = new GetObjectArgs()
                .WithBucket(_artefactBucket)
                .WithObject(artefact.Id.ToString())
                .WithFile(Path.Combine(artefactFetchDir, artefact.OriginalFilename));
            
            await minioClient.GetObjectAsync(args, cancellationToken);
        }

        if (extractArchive)
        {
            if (artefact.Type != ArtefactType.ZipArchive)
                throw new InvalidOperationException("Can only extract ZIP archives!");
            string extractionDir = Path.Combine(artefactFetchDir, artefact.OriginalFilename + "_extracted");
            
            var extractionProcess = System.Diagnostics.Process.Start(_unzipBinary, new List<string>() {
                Path.Combine(artefactFetchDir, artefact.OriginalFilename),
                "-d", extractionDir
            });
            await extractionProcess.WaitForExitAsync(cancellationToken);
            return extractionDir;
        }
        else
        {
            return Path.Combine(artefactFetchDir, artefact.OriginalFilename);
        }
    }

    // TODO: merge into the base class, since Digitalization.Process does the same thing?
    public async Task<string> WaitForInput(string statusDetail, CancellationToken cancellationToken)
    {
        if (Status != ProcessStatus.Running)
            throw new InvalidOperationException("Tried to wait when not running.");
        Status = ProcessStatus.WaitingForInput;
        StatusDetail = statusDetail;

        var result = await _inputChannel.Reader.ReadAsync(cancellationToken);

        Status = ProcessStatus.Running;
        StatusDetail = String.Empty;
        return result;
    }
}