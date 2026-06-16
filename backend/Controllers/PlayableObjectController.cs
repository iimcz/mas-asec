using asec.Emulation;
using asec.LongRunning;
using asec.Models;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asec.Controllers;

/// <summary>
/// Controller handling working with existing <see cref="GamePackage"/>s and starting emulations using them.
/// </summary>
[ApiController]
[Route("/api/v1/packages")]
public class PlayableObjectController : ControllerBase
{
    private AsecDBContext _dbContext;
    private IServiceScopeFactory _serviceScopeFactory;
    private IProcessManager<BaseProcess, EmulationResult, EmulationProcessDetail> _processManager;

    private readonly string _ffmpegPath;
    private readonly string _emulationBaseDirs;
    private readonly string _mainDisplay;
    private readonly string _emulationStreamBaseUrl;
    private readonly string _webcamDevice;
    private readonly string _eaasDiskInputDrive;

    public PlayableObjectController(IConfiguration configuration, AsecDBContext dbContext, IServiceScopeFactory serviceScopeFactory, IProcessManager<BaseProcess, EmulationResult, EmulationProcessDetail> processManager)
    {
        _dbContext = dbContext;
        _serviceScopeFactory = serviceScopeFactory;
        _processManager = processManager;

        var section = configuration.GetSection("Emulation");
        _ffmpegPath = section.GetValue<string>("FfmpegPath");
        _emulationBaseDirs = section.GetValue<string>("ProcessBaseDir");
        _mainDisplay = section.GetValue<string>("MainDisplay");
        _emulationStreamBaseUrl = section.GetValue<string>("StreamOutBaseUrl");
        _webcamDevice = section.GetValue<string>("WebcamDevice");
        _eaasDiskInputDrive = section.GetValue<string>("EaasDiskInputDrive");
    }

    /// <summary>
    /// Get the details of the specified GamePackage.
    /// </summary>
    /// <param name="packageId">ID of the GamePackage</param>
    /// <returns>Details of the GamePackage</returns>
    [HttpGet("{packageId}")]
    [Produces(typeof(PlayableObject))]
    public async Task<IActionResult> GetPlayableObject(string packageId)
    {
        var id = Guid.Parse(packageId);
        var package = await _dbContext.DigitalObjects
            .OfType<Models.Emulation.PlayableObject>()
            .Include(p => p.IncludedDigitalObjects)
            .Include(p => p.Environment)
            .Include(p => p.Version)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (package == null)
            return NotFound();
        return Ok(PlayableObject.FromDBEntity(package));
    }

    /// <summary>
    /// Update GamePackage metadata.
    /// </summary>
    /// <param name="packageId">ID of the GamePackage to update</param>
    /// <param name="inPackage">Metadata to update</param>
    /// <returns>The updated GamePackage</returns>
    [HttpPost("{packageId}")]
    [Produces(typeof(PlayableObject))]
    public async Task<IActionResult> UpdatePlayableObject(string packageId, [FromBody] PlayableObjectUpdate inObject)
    {
        var id = Guid.Parse(packageId);
        var package = await _dbContext.DigitalObjects
            .OfType<Models.Emulation.PlayableObject>()
            .Include(p => p.IncludedDigitalObjects)
            .Include(p => p.Environment)
            .Include(p => p.Version)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (package == null)
            return NotFound();

        // We only update the name and note here.
        package.Label = inObject.Label;
        package.InternalNote = inObject.Note;

        await _dbContext.SaveChangesAsync();
        return Ok(PlayableObject.FromDBEntity(package));
    }

    /// <summary>
    /// Start emulation for the specified game package. Makes the appropriate setup using EaaS.
    /// </summary>
    /// <param name="packageId">ID of the package to start</param>
    /// <returns>State of the emulation</returns>
    [HttpPost("{packageId}/emulate")]
    [Produces(typeof(EmulationProcess))]
    public async Task<IActionResult> EmulatePlayableObject(string packageId)
    {
        var id = Guid.Parse(packageId);
        var package = await _dbContext.DigitalObjects.OfType<Models.Emulation.PlayableObject>().Include(p => p.Environment).FirstOrDefaultAsync(p => p.Id == id);
        if (package == null)
            return NotFound();

        var config = new EmulationConfig() {
            DirsBase = _emulationBaseDirs,
            FfmpegPath = _ffmpegPath,
            MainDisplay = _mainDisplay,
            StreamBaseUrl = _emulationStreamBaseUrl,
            WebcamDevice = _webcamDevice,
            EaasTargetInputDrive = _eaasDiskInputDrive,
        };
        var process = new KioskProcess(
            package.Id,
            _serviceScopeFactory,
            config
        );
        _processManager.StartProcess(process);
        return Ok(EmulationProcess.FromProcess(process));
    }
}
