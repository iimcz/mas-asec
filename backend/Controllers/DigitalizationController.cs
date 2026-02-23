using asec.Digitalization;
using asec.LongRunning;
using asec.Models;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;

namespace asec.Controllers;

/// <summary>
/// Controller handling the process of digitalizetion of physical media to create their
/// artefacts. Manages process creation and management for digitalization and the upload
/// of the resulting artefacts to persistent storage.
/// </summary>
[ApiController]
[Route("/api/v1/digitalization")]
public class DigitalizationController : ControllerBase
{
    private readonly string _minioArtefactBucket;
    private readonly string _digitalizationDirsBase;
    private readonly ILogger<DigitalizationController> _logger;
    private readonly IToolRepository _tools;
    private readonly IProcessManager<Process, DigitalizationResult> _processManager;
    private readonly IMinioClient _minioClient;
    private readonly AsecDBContext _dbContext;

    public DigitalizationController(ILogger<DigitalizationController> logger, IToolRepository tools, IProcessManager<Process, DigitalizationResult> processManager, IMinioClient minioClient, IConfiguration config, AsecDBContext dbContext)
    {
        _logger = logger;
        _tools = tools;
        _processManager = processManager;
        _minioClient = minioClient;
        _dbContext = dbContext;
        _minioArtefactBucket = config.GetSection("ObjectStorage").GetValue<string>("ArtefactBucket");
        _digitalizationDirsBase = config.GetSection("Digitalization").GetValue<string>("ProcessBaseDir");
    }

    /// <summary>
    /// Acquire a list of available digitalization tools. This will depend on both configuration and actual
    /// availability of physical devices needed by the various tool implementations for digitalization.
    /// </summary>
    /// <returns>Enumerable of available tools</returns>
    [HttpGet("tools")]
    [Produces(typeof(IEnumerable<DigitalizationTool>))]
    public IActionResult GetDigitalizationTools()
    {
        var result = _tools.GetDigitalizationTools().Select(tool => new DigitalizationTool (
            tool.Id.ToString(),
            tool.Slug,
            tool.Name,
            tool.Version,
            tool.PhysicalMedia.ToString(),
            tool.IsAvailable
        ));
        return Ok(result);
    }

    /// <summary>
    /// Get a specific digitalization tool using its ID. To get a list of available IDs, see <see cref="GetDigitalizationTools"/>
    /// </summary>
    /// <param name="toolId">ID of the requested tool</param>
    /// <returns>Details of the digitalization tool</returns>
    [HttpGet("tools/{toolId}")]
    [Produces(typeof(DigitalizationTool))]
    public IActionResult GetDigitalizationTool(string toolId)
    {
        var tool = _tools.GetDigitalizationTool(toolId);
        return Ok(new DigitalizationTool(
            tool.Id.ToString(),
            tool.Slug,
            tool.Name,
            tool.Version,
            tool.PhysicalMedia.ToString(),
            tool.IsAvailable
        ));
    }

    /// <summary>
    /// Start the process of digitizing a physical media using the tool specified in the digitalization request. The request
    /// includes reference to a <see cref="ViewModels.Version"/> for which the media should be digitalized.
    /// </summary>
    /// <param name="request">Digitalization request details including tool and version</param>
    /// <returns>Details of the started digitalization process</returns>
    [HttpPut("start")]
    [Produces(typeof(DigitalizationProcess))]
    public async Task<IActionResult> StartDigitalizationProcess([FromBody] DigitalizationRequest request)
    {
        var tool = _tools.GetDigitalizationTool(request.ToolId);
        if (tool == null)
            return NotFound();

        // TODO: write this a bit nicer maybe
        var version = request.VersionId is null ? null : await _dbContext.WorkVersions.FindAsync(Guid.Parse(request.VersionId));
        var paratext = request.ParatextId is null ? null : await _dbContext.Paratexts.FindAsync(Guid.Parse(request.ParatextId));
        if (version == null && paratext == null)
            return NotFound();

        var process = new Process(tool, version, paratext, _digitalizationDirsBase);
        _processManager.StartProcess(process);
        return base.Ok(DigitalizationProcess.FromProcess(process));
    }

    /// <summary>
    /// Finalize a successful digitalization process to persist the results and create the resulting <see cref="Artefact"/>.
    /// </summary>
    /// <param name="processId">ID of the process to finalize</param>
    /// <param name="artefact">Details to include in the created artefact</param>
    /// <returns>The created artefact</returns>
    [HttpPost("{processId}/finalize")]
    [Produces(typeof(Artefact))]
    public async Task<IActionResult> FinalizeVersionArtifact(string processId, [FromBody] Artefact artefact)
    {
        var id = Guid.Parse(processId);
        var process = _processManager.GetProcess(id);
        if (process == null)
            return NotFound();

        var processResult = await _processManager.FinishProcessAsync(id);
        var objectId = Guid.NewGuid();
        var args = new PutObjectArgs()
            .WithFileName(processResult.Filename)
            .WithBucket(_minioArtefactBucket) // TODO: add digitalization metadata tags
            .WithObject(objectId.ToString());
        // TODO: check for success (or maybe exception?)
        var artefactObject = await _minioClient.PutObjectAsync(args);

        var dbArtefact = await artefact.ToDBEntity(_dbContext);
        dbArtefact.ObjectId = objectId;
        dbArtefact.Type = processResult.Type;
        dbArtefact.FileName = Path.GetFileName(processResult.Filename);
        dbArtefact.ArchivationDate = process.StartTime;
        dbArtefact.PhysicalMediaType = process.DigitalizationTool.PhysicalMedia;
        dbArtefact.DigitalizationTool = await _dbContext.DigitalizationTools.FindAsync(process.DigitalizationTool.Id);
        // TODO: overwrite other info with the process equivalents
        
        var paratext = await _dbContext.Paratexts.FindAsync(process.ParatextId);
        var version = await _dbContext.WorkVersions.FindAsync(process.VersionId);
        dbArtefact.Paratexts = new List<asec.Models.Archive.Paratext>();
        dbArtefact.Versions = new List<asec.Models.Archive.WorkVersion>();

        if (paratext is not null)
            dbArtefact.Paratexts.Append(paratext);
        if (version is not null)
            dbArtefact.Versions.Append(version);
        
        await _dbContext.DigitalObjects.AddAsync(dbArtefact);
        await _dbContext.SaveChangesAsync();
        _processManager.RemoveProcess(process);

        return Ok(Artefact.FromDBEntity(dbArtefact));
    }

    /// <summary>
    /// If a digitalization process is in the <see cref="ProcessStatus.WaitingForInput"/> state, provide the input it is waiting for
    /// in the form of a string.
    /// </summary>
    /// <param name="processId">ID of the process to provide input for</param>
    /// <param name="input">Data of the input</param>
    /// <returns>The process for which input was provided</returns>
    [HttpPost("{processId}/input")]
    [Produces(typeof(DigitalizationProcess))]
    public async Task<IActionResult> ProvideDigitalizationInput(string processId, [FromBody] DigitalizationInput input)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        if (process == null)
            return NotFound();

        await process.InputChannel.WriteAsync(input.Data);
        return Ok();
    }

    /// <summary>
    /// Get the text log file of a running digitalization process
    /// </summary>
    /// <param name="processId">ID of the process</param>
    /// <returns>Log file of the process as text/plain</returns>
    [HttpGet("{processId}/log")]
    // TODO: add file content-disposition headers for the log download button
    public IActionResult GetProcessLog(string processId)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        if (process == null)
            return NotFound();
        return PhysicalFile(process.LogPath, "text/plain", true);
    }

    /// <summary>
    /// Request that a digitalization process is restarted. This will result in the current process being cancelled
    /// and a new process being started with the same arguments.
    /// </summary>
    /// <param name="processId">ID of the process to restart</param>
    /// <returns>Details of the new process</returns>
    [HttpPost("{processId}/restart")]
    [Produces(typeof(DigitalizationProcess))]
    public async Task<IActionResult> RestartDigitalizationProcess(string processId)
    {
        // TODO: keep process id, since this should technically be the same process
        var process = _processManager.GetProcess(Guid.Parse(processId));
        if (process == null)
            return NotFound();
        var tool = process.DigitalizationTool;
        var version = await _dbContext.WorkVersions.FindAsync(process.VersionId);
        var paratext = await _dbContext.Paratexts.FindAsync(process.ParatextId);
        if (version is null && paratext is null)
            return NotFound();
        await _processManager.CancelProcessAsync(process.Id);
        _processManager.RemoveProcess(process);
        var newProcess = new Process(tool, version, paratext, _digitalizationDirsBase);
        _processManager.StartProcess(newProcess);
        return Ok(DigitalizationProcess.FromProcess(newProcess));
    }

    /// <summary>
    /// Get the status of the specified digitalization process.
    /// </summary>
    /// <param name="processId">ID of the process</param>
    /// <returns>Details of the digitalization process</returns>
    [HttpGet("{processId}/status")]
    [Produces(typeof(DigitalizationProcess))]
    public IActionResult GetDigitalizationProcessStatus(string processId)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        return Ok(DigitalizationProcess.FromProcess(process));
    }

    /// <summary>
    /// Request that a running digitalization process is stopped. No artefact from this process will be saved.
    /// </summary>
    /// <param name="processId">ID of the process to stop</param>
    /// <returns>Details of the stopped process</returns>
    [HttpPost("{processId}/stop")]
    [Produces(typeof(DigitalizationProcess))]
    public async Task<IActionResult> StopDigitalizationProcess(string processId)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        if (process == null)
            return NotFound();
        await _processManager.CancelProcessAsync(process.Id);
        return Ok(DigitalizationProcess.FromProcess(process));
    }

    /// <summary>
    /// Upload a file if this is requested by the digitalization process.
    /// </summary>
    /// <param name="processId">ID of the process to upload a file for</param>
    /// <param name="uploadId">ID of the upload (should be provided by the process in status details)</param>
    /// <param name="file">The uploaded file</param>
    /// <returns>Detail of the process for which the file was uploaded</returns>
    [HttpPost("{processId}/upload/{uploadId}")]
    [Produces(typeof(DigitalizationProcess))]
    public async Task<IActionResult> UploadDigitalizationFile(string processId, string uploadId, [FromForm] IFormFile file)
    {
        // TODO: proper upload handling
        var process = _processManager.GetProcess(Guid.Parse(processId));
        if (process == null)
            return NotFound();

        using (var fileStream = new FileStream(Path.Combine(process.UploadDir, uploadId), FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
            // TODO: notify process of the upload
        }

        return Ok(DigitalizationProcess.FromProcess(process));
    }

    // TODO: implement a way to get the currently running processes in case the client
    // doesn't have a specific ID stored
    public void GetRunningProcesses()
    {
        // TODO...
    }
}
