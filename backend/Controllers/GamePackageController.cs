using asec.Compatibility.EaasApi;
using asec.Compatibility.EaasApi.Models;
using asec.Emulation;
using asec.LongRunning;
using asec.Models;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/packages")]
public class GamePackageController : ControllerBase
{
    private AsecDBContext _dbContext;
    private IServiceScopeFactory _serviceScopeFactory;
    private IProcessManager<Process, EmulationResult> _processManager;

    private readonly string _ffmpegPath;
    private readonly string _emulationBaseDirs;
    private readonly string _mainDisplay;
    private readonly string _emulationStreamBaseUrl;

    public GamePackageController(IConfiguration configuration, AsecDBContext dbContext, IServiceScopeFactory serviceScopeFactory, IProcessManager<Process, EmulationResult> processManager)
    {
        _dbContext = dbContext;
        _serviceScopeFactory = serviceScopeFactory;
        _processManager = processManager;

        var section = configuration.GetSection("Emulation");
        _ffmpegPath = section.GetValue<string>("FfmpegPath");
        _emulationBaseDirs = section.GetValue<string>("ProcessBaseDir");
        _mainDisplay = section.GetValue<string>("MainDisplay");
        _emulationStreamBaseUrl = section.GetValue<string>("StreamOutBaseUrl");
    }

    [HttpGet("{packageId}")]
    [Produces(typeof(GamePackage))]
    public async Task<IActionResult> GetGamePackage(string packageId)
    {
        var id = Guid.Parse(packageId);
        var package = await _dbContext.GamePackages
            .Include(p => p.IncludedArtefacts)
            .Include(p => p.Environment)
            .Include(p => p.Version)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (package == null)
            return NotFound();
        return Ok(GamePackage.FromGamePackage(package));
    }

    [HttpPost("{packageId}")]
    [Produces(typeof(GamePackage))]
    public async Task<IActionResult> UpdateGamePackage(string packageId, [FromBody] GamePackage inPackage)
    {
        var id = Guid.Parse(packageId);
        var package = await _dbContext.GamePackages
            .Include(p => p.IncludedArtefacts)
            .Include(p => p.Environment)
            .Include(p => p.Version)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (package == null)
            return NotFound();
        // We only update the name here.
        package.Name = inPackage.Name;
        await _dbContext.SaveChangesAsync();
        return Ok(GamePackage.FromGamePackage(package));
    }

    [HttpPost("{packageId}/emulate")]
    public async Task<IActionResult> EmulateGamePackage(string packageId)
    {
        var id = Guid.Parse(packageId);
        var package = await _dbContext.GamePackages.Include(p => p.Environment).FirstOrDefaultAsync(p => p.Id == id);
        if (package == null)
            return NotFound();
        
        var process = new Process(
            package.Id,
            _serviceScopeFactory,
            _emulationBaseDirs,
            _ffmpegPath,
            _mainDisplay,
            _emulationStreamBaseUrl);
        _processManager.StartProcess(process);
        return Ok(EmulationState.FromProcess(process));
    }

    [HttpGet("{packageId}/paratexts")]
    public async Task<IActionResult> GetPackageParatexts(string packageId)
    {
        var id = Guid.Parse(packageId);
        var package = await _dbContext.GamePackages
            .Include(p => p.Paratexts)
            .ThenInclude(p => p.Work)
            .Include(p => p.Paratexts)
            .ThenInclude(p => p.Version)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (package == null)
            return NotFound();
        return Ok(package.Paratexts.Select(p => Paratext.FromDBParatext(p)));
    }
}