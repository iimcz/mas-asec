using asec.Digitalization;
using asec.LongRunning;
using asec.Models;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/digitalization")]
public class DigitalizationController : ControllerBase
{
    private readonly string _minioArtefactBucket;
    private readonly string _digitalizationDirsBase;
    private ILogger<DigitalizationController> _logger;
    private IToolRepository _tools;
    private IProcessManager<Process> _processManager;
    private IMinioClient _minioClient;
    private AsecDBContext _dbContext;

    public DigitalizationController(ILogger<DigitalizationController> logger, IToolRepository tools, IProcessManager<Process> processManager, IMinioClient minioClient, IConfiguration config, AsecDBContext dbContext)
    {
        _logger = logger;
        _tools = tools;
        _processManager = processManager;
        _minioClient = minioClient;
        _dbContext = dbContext;
        _minioArtefactBucket = config.GetSection("ObjectStorage").GetValue<string>("ArtefactBucket");
    }

    [HttpGet("tools")]
    [Produces(typeof(IEnumerable<DigitalizationTool>))]
    public IActionResult GetDigitalizationTools()
    {
        var result = _tools.GetDigitalizationTools().Select(tool => new DigitalizationTool (
            tool.Id,
            tool.Name,
            tool.Version,
            tool.PhysicalMedia.ToString(),
            tool.IsAvailable
        ));
        return Ok(result);
    }

    [HttpPut("start")]
    [Produces(typeof(DigitalizationProcess))]
    public async Task<IActionResult> StartDigitalizationProcess([FromBody] DigitalizationRequest request)
    {
        var tool = _tools.GetDigitalizationTool(request.ToolId);
        if (tool == null)
            return NotFound();

        var version = await _dbContext.Versions.FindAsync(Guid.Parse(request.VersionId));
        if (version == null)
            return NotFound();

        var process = new Process(tool, version, _digitalizationDirsBase);
        _processManager.StartProcess(process);
        return base.Ok(ViewModels.DigitalizationProcess.FromProcess(process));
    }

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
            .WithFileName(processResult)
            .WithBucket(_minioArtefactBucket)
            .WithObject(objectId.ToString());
        // TODO: check for success (or maybe exception?)
        var artefactObject = await _minioClient.PutObjectAsync(args);

        artefact.VersionId = process.VersionId.ToString();
        var dbArtefact = await artefact.ToDBEntity(_dbContext);
        dbArtefact.Id = objectId;
        
        await _dbContext.Artefacts.AddAsync(dbArtefact);
        await _dbContext.SaveChangesAsync();

        return Ok(Artefact.FromDBEntity(dbArtefact));
    }

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

    [HttpGet("{processId}/log")]
    public IActionResult GetProcessLog(string processId)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        return PhysicalFile(process.LogPath, "text/plain", true);
    }

    [HttpPost("{processId}/restart")]
    [Produces(typeof(DigitalizationProcess))]
    public async Task<IActionResult> RestartDigitalizationProcess(string processId)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        if (process == null)
            return NotFound();
        var tool = process.DigitalizationTool;
        var version = await _dbContext.Versions.FindAsync(process.VersionId);
        if (version == null)
            return NotFound();
        await _processManager.CancelProcessAsync(process.Id);
        var newProcess = new Process(tool, version, _digitalizationDirsBase);
        _processManager.StartProcess(newProcess);
        return base.Ok(DigitalizationProcess.FromProcess(newProcess));
    }

    [HttpGet("{processId}/status")]
    [Produces(typeof(DigitalizationProcess))]
    public IActionResult GetDigitalizationProcessStatus(string processId)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        return base.Ok(DigitalizationProcess.FromProcess(process));
    }

    [HttpPost("{processId}/stop")]
    [Produces(typeof(DigitalizationProcess))]
    public async Task<IActionResult> StopDigitalizationProcess(string processId)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        if (process == null)
            return NotFound();
        await _processManager.CancelProcessAsync(process.Id);
        return base.Ok(DigitalizationProcess.FromProcess(process));
    }

    [HttpPost("{processId}/upload/{uploadId}")]
    [Produces(typeof(DigitalizationProcess))]
    public async Task<IActionResult> UploadDigitalizationFile(string processId, string uploadId)
    {
        // TODO: proper upload handling
        var process = _processManager.GetProcess(Guid.Parse(processId));
        if (process == null)
            return NotFound();

        var stream = Request.Body;
        using (var fileStream = new FileStream(Path.Combine(process.UploadDir, uploadId), FileMode.Create))
        {
            await stream.CopyToAsync(fileStream);
            // TODO: notify process of the upload
        }

        return base.Ok(DigitalizationProcess.FromProcess(process));
    }
}