using asec.LongRunning;
using asec.DataConversion;
using Microsoft.AspNetCore.Mvc;
using asec.ViewModels;
using asec.Emulation;
using asec.Models;
using Microsoft.EntityFrameworkCore;
using asec.Compatibility.EaasApi;
using asec.Compatibility.EaasApi.Models;
using asec.Models.Digitalization;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/conversion")]
public class ConversionController : ControllerBase
{
    private readonly string _conversionDirsBase;
    private readonly string _artefactBucket;
    private readonly string _unzipBinary;
    private ILogger<ConversionController> _logger;
    private AsecDBContext _dbContext;
    private IProcessManager<DataConversion.Process, ConversionResult> _processManager;
    private IEmulatorRepository _emulatorRepository;
    private IServiceScopeFactory _serviceScopeFactory;

    // EaaS clients
    private ObjectRepositoryClient _eaasObjectRepoClient;
    private EaasUploadClient _eaasUploadClient;

    public ConversionController(
        ILogger<ConversionController> logger,
        AsecDBContext dbContext,
        IProcessManager<DataConversion.Process, ConversionResult> processManager,
        IEmulatorRepository emulatorRepository,
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration config,
        EaasUploadClient eaasUploadClient,
        ObjectRepositoryClient eaasObjectRepositoryClient)
    {
        _logger = logger;
        _dbContext = dbContext;
        _processManager = processManager;
        _emulatorRepository = emulatorRepository;
        _serviceScopeFactory = serviceScopeFactory;

        _eaasUploadClient = eaasUploadClient;
        _eaasObjectRepoClient = eaasObjectRepositoryClient;

        var section = config.GetSection("Conversion");
        _unzipBinary = section.GetValue<string>("UnzipBinary");
        _conversionDirsBase = section.GetValue<string>("ProcessBaseDir");
        _artefactBucket = config.GetSection("ObjectStorage").GetValue<string>("ArtefactBucket");
    }

    [HttpPut("start")]
    [Produces(typeof(ConversionProcess))]
    public async Task<IActionResult> StartConversionForEmulator([FromBody] ConversionRequest conversionRequest)
    {
        var environmentId = Guid.Parse(conversionRequest.EmulatorId);
        var artefactIds = conversionRequest.ArtefactIds.Select(id => Guid.Parse(id));
        var artefacts = await _dbContext.Artefacts.Where(a => artefactIds.Contains(a.Id)).ToListAsync();

        var artefactType = artefacts[0].Type;
        if (!artefacts.All(a => a.Type == artefactType))
            return BadRequest();
        var converter = await _emulatorRepository.GetEnvironmentConverterAsync(environmentId, artefactType);
        if (converter == null)
            return NotFound();

        var process = new DataConversion.Process(environmentId, converter, artefacts, _serviceScopeFactory, _conversionDirsBase, _artefactBucket, _unzipBinary);
        _processManager.StartProcess(process);
        
        return Ok(ConversionProcess.FromProcess(process));
    }

    [HttpPost("{processId}/finalize")]
    [Produces(typeof(GamePackage))]
    public async Task<IActionResult> FinalizeConversionProcess(string processId, [FromBody] GamePackage package, CancellationToken cancellationToken = default)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        if (process == null)
            return NotFound();

        var result = await _processManager.FinishProcessAsync(process.Id);
        _processManager.RemoveProcess(process);

        // TODO: do better error checking
        var eaasUploadTasks = result.Files.Select(f => _eaasUploadClient.Upload(f.Filename, cancellationToken));
        var eaasUploadResponses = await Task.WhenAll(eaasUploadTasks);
        if (!eaasUploadResponses.All(r => r.status == "0"))
        {
            _logger.LogError("Failed to upload some objects to EaaS:");
            foreach (var resp in eaasUploadResponses)
                _logger.LogError("{}", resp);
        }

        // TODO: use a proper file format (ImportFileInfo.fileFmt)
        var importObjects = eaasUploadResponses.SelectMany(
            r => r.uploadedItemList
        ).Select(
            uploaded => new ImportFileInfo(uploaded.url, DeviceIdFromType(process.Artefacts[0].Type), string.Empty, uploaded.filename)
        ).ToList();

        // TODO: better label?
        var eaasObjectId = await _eaasObjectRepoClient.ImportObjects(new(
            $"converted[{process.Id}]",
            importObjects
        ), cancellationToken);

        var artefactIds = process.Artefacts.Select(a => a.Id).ToList();
        var version = (await _dbContext.Artefacts
            .Include(a => a.Version)
            .FirstOrDefaultAsync(a => a.Id == artefactIds[0], cancellationToken))?.Version;
        var dbGamePackage = new Models.Emulation.GamePackage() {
            Id = Guid.Parse(eaasObjectId),
            Name = package.Name,
            ConversionDate = process.StartTime,
            Converter = await _dbContext.Converters.FindAsync(process.Converter.Id),
            Environment = await _dbContext.Environments.FindAsync(process.EnvironmentId),
            IncludedArtefacts = await _dbContext.Artefacts.Where(a => artefactIds.Contains(a.Id)).ToListAsync(cancellationToken),
            Version = version
        };
        _dbContext.GamePackages.Add(dbGamePackage);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(GamePackage.FromGamePackage(dbGamePackage));
    }

    [HttpPost("{processId}/input")]
    [Produces(typeof(ConversionProcess))]
    public async Task<IActionResult> ProvideConversionInput(string processId, [FromBody] ConversionInput input)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        if (process == null)
            return NotFound();
        
        await process.InputChannel.WriteAsync(input.Data);
        return Ok();
    }

    [HttpGet("{processId}/log")]
    public IActionResult GetConversionProcessLog(string processId)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        if (process == null)
            return NotFound();
        return PhysicalFile(process.LogPath, "text/plain");
    }

    [HttpPost("{processId}/restart")]
    [Produces(typeof(ConversionProcess))]
    public async Task<IActionResult> RestartConversionProcess(string processId)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        if (process == null)
            return NotFound();
        
        await _processManager.CancelProcessAsync(process.Id);
        _processManager.RemoveProcess(process);

        var newProcess = new DataConversion.Process(process.EnvironmentId, process.Converter, process.Artefacts, _serviceScopeFactory, _conversionDirsBase, _artefactBucket, _unzipBinary);
        _processManager.StartProcess(newProcess);

        return Ok(ConversionProcess.FromProcess(newProcess));
    }

    [HttpGet("{processId}/status")]
    [Produces(typeof(ConversionProcess))]
    public IActionResult GetConversionProcessStatus(string processId)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        if (process == null)
            return NotFound();
        return Ok(ConversionProcess.FromProcess(process));
    }

    [HttpPost("{processId}/stop")]
    [Produces(typeof(ConversionProcess))]
    public async Task<IActionResult> StopConversionProcess(string processId)
    {
        var id = Guid.Parse(processId);
        var process = _processManager.GetProcess(id);
        if (process == null)
            return NotFound();
        await _processManager.CancelProcessAsync(id);
        return Ok(ConversionProcess.FromProcess(process));
    }

    private static string DeviceIdFromType(ArtefactType _)
    {
        // TODO: For now always use Files. In the future it would be nice
        // to properly specify the device. This is here to ensure even a floppy
        // gets presented to the virtual machine as an .img file, instead of
        // an inserted floppy disk. That would require also checking if the format
        // is supported by QEMU and wouldn't allow differently formatted floppies
        // for different platforms/emulators.
        return DeviceID.Files;
    }
}