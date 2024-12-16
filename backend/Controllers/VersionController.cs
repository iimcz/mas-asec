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
    /// Create a new work version with the specified details (a new ID will be generated).
    /// </summary>
    /// <param name="version">Details of the version to create</param>
    /// <returns>The newly created version</returns>
    [HttpPut]
    [Produces(typeof(ViewModels.Version))]
    public async Task<IActionResult> AddVersion([FromBody] ViewModels.Version version)
    {
        // Ignore incoming Id, a new one will be generated anyway
        version.Id = String.Empty;
        var dbVersion = await version.ToDBEntity(_dbContext, true);
        dbVersion.Id = Guid.NewGuid();

        await _dbContext.Versions.AddAsync(dbVersion);
        await _dbContext.SaveChangesAsync();
        _logger.LogDebug("New Version created: {0}", dbVersion.Id.ToString());

        return Ok(ViewModels.Version.FromDBEntity(dbVersion));
    }

    /// <summary>
    /// Get the details of the specified work version.
    /// </summary>
    /// <param name="versionId">ID of the version</param>
    /// <returns>Details of the version</returns>
    [HttpGet("{versionId}")]
    [Produces(typeof(ViewModels.Version))]
    public async Task<IActionResult> GetVersion(string versionId)
    {
        var result = await _dbContext.Versions
            .Include(v => v.Status)
            .Include(v => v.System)
            .Include(v => v.Work)
            .FirstOrDefaultAsync(v => v.Id == Guid.Parse(versionId));
        if (result == null)
            return NotFound();
        return Ok(ViewModels.Version.FromDBEntity(result));
    }

    /// <summary>
    /// Update the specified version with new details.
    /// </summary>
    /// <param name="versionId">ID of the version to update</param>
    /// <param name="version">New version details</param>
    /// <returns>The updated work version</returns>
    [HttpPost("{versionId}")]
    [Produces(typeof(ViewModels.Version))]
    public async Task<IActionResult> UpdateVersion(string versionId, [FromBody] ViewModels.Version version)
    {
        var id = Guid.Parse(versionId);
        if (await _dbContext.Versions.FindAsync(id) == null)
            return NotFound();
        
        _logger.LogDebug("Version found, updating Version id: {0}", versionId);
        version.Id = versionId;
        var dbVersion = await version.ToDBEntity(_dbContext, true);

        await _dbContext.SaveChangesAsync();
        return Ok(ViewModels.Version.FromDBEntity(dbVersion));
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
        var version = await _dbContext.Versions
            .Include(v => v.Artefacts)
            .ThenInclude(a => a.DigitalizationTool)
            .FirstOrDefaultAsync(v => v.Id == id);
        if (version == null)
            return NotFound();
        return Ok(version.Artefacts.Select(a => ViewModels.Artefact.FromDBEntity(a)));
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
        var version = await _dbContext.Versions
            .Include(v => v.GamePackages)
            .ThenInclude(p => p.IncludedArtefacts)
            .Include(v => v.GamePackages)
            .ThenInclude(p => p.Environment)
            .FirstOrDefaultAsync(v => v.Id == id);
        if (version == null)
            return NotFound();
        var packages = version.GamePackages.Select(p => GamePackage.FromGamePackage(p));
        return Ok(packages);
    }

    /// <summary>
    /// Add a new paratext with the specified details to the specified version.
    /// </summary>
    /// <param name="versionId">ID of the version</param>
    /// <param name="paratext">Details of the paratext</param>
    /// <returns>The newly created paratext</returns>
    [HttpPut("{versionId}/paratexts")]
    [Produces(typeof(Paratext))]
    public async Task<IActionResult> AddVersionParatext(string versionId, [FromBody] Paratext paratext)
    {
        var id = Guid.Parse(versionId);
        var version = await _dbContext.Versions.Include(v => v.Work).FirstOrDefaultAsync(v => v.Id == id);
        if (version == null)
            return NotFound();
        var newParatext = new Models.Archive.Paratext() {
            Id = Guid.NewGuid(),
            Work = version.Work,
            Version = version,
            Name = paratext.Name,
            Description = paratext.Description,
            Source = paratext.Source,
            SourceUrl = paratext.SourceUrl,
            Thumbnail = "template:unknown",
            Downloadable = false,
        };
        _dbContext.Paratexts.Add(newParatext);
        await _dbContext.SaveChangesAsync();
        return Ok(Paratext.FromDBParatext(newParatext));
    }

    /// <summary>
    /// Get all paratexts for the specified version.
    /// </summary>
    /// <param name="versionId">ID of the version</param>
    /// <returns>Enumerable of the available paratexts</returns>
    [HttpGet("{versionId}/paratexts")]
    [Produces(typeof(IEnumerable<Paratext>))]
    public async Task<IActionResult> GetVersionParatexts(string versionId)
    {
        var id = Guid.Parse(versionId);
        var version = await _dbContext.Versions
            .Include(v => v.Paratexts)
            .ThenInclude(p => p.Work)
            .Include(v => v.Paratexts)
            .ThenInclude(p => p.GamePackage)
            .FirstOrDefaultAsync(v => v.Id == id);
        if (version == null)
            return NotFound();
        return Ok(version.Paratexts.Select(p => Paratext.FromDBParatext(p)));
    }
}