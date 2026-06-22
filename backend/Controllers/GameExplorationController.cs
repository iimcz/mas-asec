using asec.Models;
using asec.Models.Emulation;
using asec.ViewModels;
using asec.LongRunning;
using asec.Exploration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.HighPerformance.Helpers;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/preparation")]
public class GameExplorationController : ControllerBase
{
    private readonly ILogger<GameExplorationController> _logger;
    private readonly AsecDBContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private IProcessManager<Exploration.Process, ExplorationResult, ExplorationProcessDetail> _processManager;

    public GameExplorationController(
            ILogger<GameExplorationController> logger,
            AsecDBContext dbContext,
            IServiceScopeFactory serviceScopeFactory,
            IProcessManager<Exploration.Process, ExplorationResult, ExplorationProcessDetail> processManager,
            IConfiguration configuration)
    {
        _logger = logger;
        _dbContext = dbContext;
        _configuration = configuration;

        _serviceScopeFactory = serviceScopeFactory;
        _processManager = processManager;
    }

    [HttpGet("environments")]
    public async Task<IActionResult> GetEnvironments()
    {
        return Ok(_dbContext.Environments
            .Where(e => e.Type == EnvironmentType.Exploration)
            .Select(e => ExplorationEnvironment.FromEmulationEnvironment(e)));
    }

    [HttpGet("kiosks")]
    public async Task<IActionResult> GetKiosks()
    {
        return Ok(_dbContext.Environments
            .Where(e => e.Type == EnvironmentType.Kiosk)
            .Select(e => ExplorationEnvironment.FromEmulationEnvironment(e)));
    }

    [HttpPost("start")]
    [Produces(typeof(ExplorationProcess))]
    public async Task<IActionResult> StartExploringObjects([FromBody] ExplorationRequest request)
    {
        var id = Guid.Parse(request.EnvironmentId);
        var environment = await _dbContext.Environments
            .Where(e => e.Type == EnvironmentType.Exploration)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (environment == null)
            return NotFound();
        var versionId = Guid.Parse(request.VersionId);
        var version = await _dbContext.WorkVersions
            .FindAsync(versionId);
        if (version == null)
            return NotFound();

        var digiObjectIds = request.DigitalObjectIds.Select(id => Guid.Parse(id));
        var digiObjects = _dbContext.DigitalObjects.Where(o => digiObjectIds.Contains(o.Id));
        var artefacts = digiObjects.OfType<Models.Digitalization.Artefact>();

        var process = new Exploration.Process(
            _configuration,
            _serviceScopeFactory,
            id,
            artefacts.ToList(),
            version
        );
        _processManager.StartProcess(process);
        return Ok(ExplorationProcess.FromProcess(process));
    }

    [HttpPost("{explorationId}/input/ping")]
    [Produces(typeof(ExplorationProcess))]
    public async Task<IActionResult> PingExplorationProcess(string explorationId)
    {
        var id = Guid.Parse(explorationId);
        var process = _processManager.GetProcess(id);
        if (process == null)
            return NotFound();
        await process.ChannelWriter.WriteAsync(Process.ExplorationMessage.Ping);
        return Ok(ExplorationProcess.FromProcess(process));
    }

    [HttpPost("{explorationId}/input/abort")]
    public async Task<IActionResult> AbortExploration(string explorationId)
    {
        var id = Guid.Parse(explorationId);
        var process = _processManager.GetProcess(id);
        if (process == null)
            return NotFound();

        // TODO: the actual abort action
        return Ok();
    }

    [HttpPost("{explorationId}/input/save")]
    [Produces(typeof(ViewModels.PlayableObject))]
    public async Task<IActionResult> SaveExploration(string explorationId, [FromBody] ExplorationDone saveInfo)
    {
        var id = Guid.Parse(explorationId);
        var process = _processManager.GetProcess(id);
        if (process == null)
            return NotFound();

        // TODO: implement properly
        return Ok();
    }

    [HttpPost("{explorationId}/input/finish")]
    [Produces(typeof(ViewModels.PlayableObject))]
    public async Task<IActionResult> FinishExploration(string explorationId, [FromBody] ExplorationDone finishInfo)
    {
        var id = Guid.Parse(explorationId);
        var process = _processManager.GetProcess(id);
        if (process == null)
            return NotFound();

        await process.ChannelWriter.WriteAsync(Process.ExplorationMessage.Done);
        var result = await _processManager.FinishProcessAsync(id);

        var artefacts = await _dbContext.DigitalObjects
            .Where(a => result.IncludedArtefactIds.Contains(a.Id))
            .ToListAsync();
        var version = await _dbContext.WorkVersions.FindAsync(result.VersionId);
        var environment = await _dbContext.Environments.FindAsync(result.PlayableObject.Environment.Id);

        var playable = result.PlayableObject;
        playable.Label = finishInfo.PackageLabel;
        playable.InternalNote = finishInfo.PackageNote;
        playable.Environment = environment;
        playable.IncludedDigitalObjects = artefacts;
        playable.WorkVersions = [version];

        _dbContext.DigitalObjects.Add(playable);
        await _dbContext.SaveChangesAsync();

        return Ok(ViewModels.PlayableObject.FromDBEntity(playable));
    }

    [HttpPost("{explorationId}/input/{type}")]
    [Produces(typeof(ExplorationProcess))]
    public async Task<IActionResult> TakeExplorationInput(string explorationId, string type)
    {
        bool correctMessage = Enum.TryParse<ViewModels.ExplorationInputType>(type, true, out var message);
        if (!correctMessage)
            return BadRequest();

        var id = Guid.Parse(explorationId);
        var process = _processManager.GetProcess(id);
        if (process == null)
            return NotFound();

        switch (message)
        {
            case ExplorationInputType.GotoCheck:
                await process.ChannelWriter.WriteAsync(Process.ExplorationMessage.GotoCheck);
                break;
            case ExplorationInputType.GotoKiosk:
                await process.ChannelWriter.WriteAsync(Process.ExplorationMessage.GotoKiosk);
                break;
            case ExplorationInputType.GotoExploration:
                await process.ChannelWriter.WriteAsync(Process.ExplorationMessage.GotoExploration);
                break;
            case ExplorationInputType.Done:
                await process.ChannelWriter.WriteAsync(Process.ExplorationMessage.Done);
                break;
        }

        await Task.Delay(100);
        return Ok(ExplorationProcess.FromProcess(process));
    }
}
