using asec.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asec.Controllers;

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

    [HttpPut]
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

    [HttpGet("{versionId}")]
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

    [HttpPost("{versionId}")]
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

    [HttpGet("{versionId}/artefacts")]
    public async Task<IActionResult> GetArtefacts(string versionId)
    {
        var id = Guid.Parse(versionId);
        var version = await _dbContext.Versions.Include(v => v.Artefacts).FirstOrDefaultAsync(v => v.Id == id);
        if (version == null)
            return NotFound();
        return Ok(version.Artefacts.Select(a => ViewModels.Artefact.FromDBEntity(a)));
    }
}