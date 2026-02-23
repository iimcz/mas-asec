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

/// <summary>
/// Controller handling the process of converting an artefact (or a set of artefacts) to a format
/// which the target intended emulator can consume.
/// </summary>
[ApiController]
[Route("/api/v1/conversion")]
public class ConversionController : ControllerBase
{
    private readonly string _conversionDirsBase;
    private readonly string _artefactBucket;
    private readonly string _unzipBinary;
    private readonly ILogger<ConversionController> _logger;
    private readonly AsecDBContext _dbContext;
    private readonly IProcessManager<DataConversion.Process, ConversionResult> _processManager;
    private readonly IEmulatorRepository _emulatorRepository;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    // EaaS clients
    private readonly ObjectRepositoryClient _eaasObjectRepoClient;
    private readonly EaasUploadClient _eaasUploadClient;

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

    /// <summary>
    /// Request that a conversion for the specified emulator be started, using the specified artefacts.
    /// This will create a process that can later be checked for status and progress, and needs to be
    /// explicitly finished for the results to persist.
    /// </summary>
    /// <param name="conversionRequest">Conversion request details, including the target emulator and used artefacts</param>
    /// <returns>Details of the newly created process</returns>
    [HttpPut("start")]
    [Produces(typeof(ConversionProcess))]
    public async Task<IActionResult> StartConversionForEmulator([FromBody] ConversionRequest conversionRequest)
    {
        var environmentId = Guid.Parse(conversionRequest.EmulatorId);
        var digiObjectIds = conversionRequest.DigitalObjectIds.Select(id => Guid.Parse(id));
        var digiObjects = _dbContext.DigitalObjects.Where(o => digiObjectIds.Contains(o.Id));
        var artefacts = digiObjects.OfType<Models.Digitalization.Artefact>();
        var firstArtefact = artefacts.FirstOrDefault();

        var versionId = Guid.Parse(conversionRequest.VersionId);
        var version = await _dbContext.WorkVersions.FirstOrDefaultAsync(v => v.Id == versionId);
        if (version is null)
            return NotFound();

        if (firstArtefact is null || await artefacts.AnyAsync(a => firstArtefact.Type != a.Type))
            return BadRequest();
        var converter = await _emulatorRepository.GetEnvironmentConverterAsync(environmentId, firstArtefact.Type);
        if (converter == null)
            return NotFound();

        var process = new DataConversion.Process(environmentId, converter, await artefacts.ToListAsync(), version, _serviceScopeFactory, _conversionDirsBase, _artefactBucket, _unzipBinary);
        _processManager.StartProcess(process);
        
        return Ok(ConversionProcess.FromProcess(process));
    }

    /// <summary>
    /// Finalize the conversion process, taking the resulting files and uploading them to EaaS in preparation to
    /// attach them to an emulator environment/VM. The finalized process is expected to be in the <see cref="ProcessStatus.Success"/> state.
    /// </summary>
    /// <param name="processId">ID of the process to finalize</param>
    /// <param name="package">Properties to apply to the resulting GamePackage</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The resulting GamePackage</returns>
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
        var dbGamePackage = new Models.Emulation.GamePackage() {
            Id = Guid.Parse(eaasObjectId),
            Name = package.Name,
            ConversionDate = process.StartTime,
            Converter = await _dbContext.Converters.FindAsync(process.Converter.Id),
            Environment = await _dbContext.Environments.FindAsync(process.EnvironmentId),
            IncludedDigitalObjects = await _dbContext.DigitalObjects.Where(a => artefactIds.Contains(a.Id)).ToListAsync(cancellationToken),
            Version = await _dbContext.WorkVersions.FindAsync(process.VersionId)
        };
        _dbContext.DigitalObjects.Add(dbGamePackage);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(GamePackage.FromGamePackage(dbGamePackage));
    }

    /// <summary>
    /// Provide a text input to the digitalization process.
    /// </summary>
    /// <param name="processId">ID of the process</param>
    /// <param name="input">Input data</param>
    /// <returns>Nothing</returns>
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

    /// <summary>
    /// Fetches the text log of a running process.
    /// </summary>
    /// <param name="processId">ID of the process</param>
    /// <returns>The process' log file as text/plain</returns>
    [HttpGet("{processId}/log")]
    public IActionResult GetConversionProcessLog(string processId)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        if (process == null)
            return NotFound();
        return PhysicalFile(process.LogPath, "text/plain");
    }

    /// <summary>
    /// Request that an existing conversion process is restarted. The system will stop the current process and
    /// create a new one with the same parameters as the original.
    /// </summary>
    /// <param name="processId">ID of the process to restart</param>
    /// <returns>Information about the new process</returns>
    [HttpPost("{processId}/restart")]
    [Produces(typeof(ConversionProcess))]
    public async Task<IActionResult> RestartConversionProcess(string processId)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        if (process is null)
            return NotFound();
        var version = await _dbContext.WorkVersions.FirstOrDefaultAsync(v => v.Id == process.VersionId);
        if (version is null)
            return NotFound();
        
        await _processManager.CancelProcessAsync(process.Id);
        _processManager.RemoveProcess(process);

        var newProcess = new DataConversion.Process(process.EnvironmentId, process.Converter, process.Artefacts, version, _serviceScopeFactory, _conversionDirsBase, _artefactBucket, _unzipBinary);
        _processManager.StartProcess(newProcess);

        return Ok(ConversionProcess.FromProcess(newProcess));
    }

    /// <summary>
    /// Fetches the current status of the specified process.
    /// </summary>
    /// <param name="processId">ID of the conversion process</param>
    /// <returns>Information about the specified process</returns>
    [HttpGet("{processId}/status")]
    [Produces(typeof(ConversionProcess))]
    public IActionResult GetConversionProcessStatus(string processId)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        if (process == null)
            return NotFound();
        return Ok(ConversionProcess.FromProcess(process));
    }

    /// <summary>
    /// Request that the specified process be stopped. This will result in discarding any data created during
    /// the process.
    /// </summary>
    /// <param name="processId">ID of the process to stop</param>
    /// <returns>Status of the stopped process</returns>
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
