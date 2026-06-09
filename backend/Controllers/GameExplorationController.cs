using asec.Models;
using asec.Models.Emulation;
using asec.ViewModels;
using asec.LongRunning;
using asec.Exploration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/preparation")]
public class GameExplorationController : ControllerBase
{
    private readonly ILogger<GameExplorationController> _logger;
    private readonly AsecDBContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly string _emulationStreamBaseUrl;
    private IServiceScopeFactory _serviceScopeFactory;
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

        _emulationStreamBaseUrl = configuration.GetSection("Emulation").GetValue<string>("StreamBaseUrl");
    }

    [HttpGet("environments")]
    public async Task<IActionResult> GetEnvironments()
    {
        return Ok(_dbContext.Environments
                .Where(e => e.Type == EnvironmentType.Exploration)
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

        var digiObjectIds = request.DigitalObjectIds.Select(id => Guid.Parse(id));
        var digiObjects = _dbContext.DigitalObjects.Where(o => digiObjectIds.Contains(o.Id));
        var artefacts = digiObjects.OfType<Models.Digitalization.Artefact>();

        var process = new Exploration.Process(
            _configuration,
            _serviceScopeFactory,
            id,
            artefacts.ToList()
        );
        _processManager.StartProcess(process);
        return Ok(ExplorationProcess.FromProcess(process));
    }

    [HttpGet("{explorationId}/ping")]
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
    [Produces(typeof(ViewModels.RunnablePackage))]
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
    [Produces(typeof(ViewModels.RunnablePackage))]
    public async Task<IActionResult> FinishExploration(string explorationId, [FromBody] ExplorationDone finishInfo)
    {
        var id = Guid.Parse(explorationId);
        var process = _processManager.GetProcess(id);
        if (process == null)
            return NotFound();

        // TODO: implement properly
        return Ok(ViewModels.RunnablePackage.FromDBEntity(process.LatestRunnablePackage));
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
            case ExplorationInputType.GotoKiosk:
                await process.ChannelWriter.WriteAsync(Process.ExplorationMessage.GotoKiosk);
                break;
            case ExplorationInputType.GotoExploration:
                await process.ChannelWriter.WriteAsync(Process.ExplorationMessage.GotoExploration);
                break;
        }

        await Task.Delay(100); // HACK: wait a bit for changes to happen
        return Ok(ExplorationProcess.FromProcess(process));
    }
}
