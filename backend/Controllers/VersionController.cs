using asec.Models;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asec.Controllers;

/// <summary>
/// Controller concerned with managing version metadata and paratexts.
/// </summary>
[ApiController]
[Route("/api/v1/versions")]
public class VersionController : ControllerBase
{
    ILogger<VersionController> _logger;
    AsecDBContext _dbContext;

    public VersionController(ILogger<VersionController> logger, AsecDBContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get the details of the specified work version.
    /// </summary>
    /// <param name="versionId">ID of the version</param>
    /// <returns>Details of the version</returns>
    [HttpGet("{versionId}")]
    [Produces(typeof(ViewModels.WorkVersion))]
    public async Task<IActionResult> GetVersion(string versionId)
    {
        var id = Guid.Parse(versionId);
        var result = await _dbContext.WorkVersions
            .FirstOrDefaultAsync(v => v.Id == id);
        if (result == null)
            return NotFound();
        return Ok(ViewModels.WorkVersion.FromDBEntity(result));
    }

    /// <summary>
    /// Get all artefacts tied to the specified work version.
    /// </summary>
    /// <param name="versionId">ID of the version</param>
    /// <returns>Enumerable of artefacts</returns>
    [HttpGet("{versionId}/artefacts")]
    [Produces(typeof(IEnumerable<Artefact>))]
    public async Task<IActionResult> GetArtefacts(string versionId)
    {
        var id = Guid.Parse(versionId);
        var version = await _dbContext.WorkVersions
            .Include(v => v.DigitalObjects)
            .FirstOrDefaultAsync(v => v.Id == id);
        if (version == null)
            return NotFound();
        return Ok(version.DigitalObjects.OfType<Models.Digitalization.Artefact>().Select(a => ViewModels.Artefact.FromDBEntity(a)));
    }

    /// <summary>
    /// Get all game packages tied to the specified work version.
    /// </summary>
    /// <param name="versionId">ID of the vesrion</param>
    /// <returns>Enumerable of game packages</returns>
    [HttpGet("{versionId}/packages")]
    [Produces(typeof(IEnumerable<GamePackage>))]
    public async Task<IActionResult> GetGamePackages(string versionId)
    {
        var id = Guid.Parse(versionId);
        var version = await _dbContext.WorkVersions
            .Include(v => v.DigitalObjects)
            .FirstOrDefaultAsync(v => v.Id == id);
        if (version == null)
            return NotFound();
        var packages = version.DigitalObjects.OfType<Models.Emulation.GamePackage>().ToList();
        packages.ForEach(p => _dbContext.Entry(p).Collection(p => p.IncludedDigitalObjects).Load());
        packages.ForEach(p => _dbContext.Entry(p).Reference(p => p.Environment).Load());
        return Ok(packages.Select(p => GamePackage.FromGamePackage(p)));
    }
}
