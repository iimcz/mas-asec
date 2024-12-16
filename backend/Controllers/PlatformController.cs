using asec.Models;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asec.Controllers;

/// <summary>
/// Controller concerned platforms and their available emulators.
/// </summary>
[ApiController]
[Route("/api/v1/platforms")]
public class PlatformController : ControllerBase
{
    private ILogger<PlatformController> _logger;
    private AsecDBContext _dbContext;

    public PlatformController(AsecDBContext dbContext, ILogger<PlatformController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all available (configured) platforms.
    /// </summary>
    /// <returns>Enumerable of available platforms</returns>
    [HttpGet]
    [Produces(typeof(IEnumerable<Platform>))]
    public async Task<IActionResult> GetPlatforms()
    {
        var platformList = await _dbContext.Platforms.ToListAsync();
        return Ok(platformList.Select(
            p => new Platform() {
                Name = p.Name,
                PhysicalMediaTypes = p.MediaTypes
                    .Select(t => t.ToString())
                    .ToList()
            }
        ));
    }

    /// <summary>
    /// Get all emulators for the specified platform.
    /// </summary>
    /// <param name="platformName">Name of the platform</param>
    /// <returns>Enumerable of available emulators</returns>
    [HttpGet("{platformName}/emulators")]
    [Produces(typeof(IEnumerable<Emulator>))]
    public async Task<IActionResult> GetEmulatorsForPlatform(string platformName)
    {
        var platform = await _dbContext.Platforms.FindAsync(platformName);
        if (platform == null)
            return NotFound();
        // TODO: There's probably a better way to do this, but we need to gather
        // environments here including their backreference to emulators and back
        // from emulators to platforms.
        var emulators = _dbContext.Emulators
            .Include(e => e.Environments)
            .ThenInclude(env => env.Emulator)
            .ThenInclude(e => e.Platforms)
            .Include(e => e.Platforms)
            .Where(e => e.Platforms.Contains(platform));
        var environments = emulators.SelectMany(e => e.Environments);

        return Ok(environments.Select(e => Emulator.FromEmulationEnvironment(e)));
    }
}