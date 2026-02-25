using asec.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asec.Controllers;

/// <summary>
/// Controller managing works, their creation, updating and their paratexts.
/// </summary>
[ApiController]
[Route("/api/v1/works")]
public class WorkController : ControllerBase
{
    private ILogger<WorkController> _logger;
    private AsecDBContext _dbContext;
    public WorkController(AsecDBContext dbContext, ILogger<WorkController> logger)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get all works in the database.
    /// </summary>
    /// <returns>Enumerable of all works</returns>
    [HttpGet]
    [Produces(typeof(IEnumerable<ViewModels.Work>))]
    public async Task<IActionResult> GetWorks()
    {
        var result = (await _dbContext.Works
            .ToListAsync())
            .Select(asec.ViewModels.Work.FromDbEntity);
        return Ok(result);
    }

    /// <summary>
    /// Get the details of the specified work.
    /// </summary>
    /// <param name="workId">ID of the work</param>
    /// <returns>Details of the work</returns>
    [HttpGet("{workId}")]
    [Produces(typeof(ViewModels.Work))]
    public async Task<IActionResult> GetWork(string workId)
    {
        var id = Guid.Parse(workId);
        var result = await _dbContext.Works
            .FirstOrDefaultAsync(w => w.Id == id);

        if (result == null)
            return NotFound();

        _logger.LogDebug("Found work with id: {0}", workId);
        return Ok(ViewModels.Work.FromDbEntity(result));
    }

    /// <summary>
    /// Get all versions of the specified work.
    /// </summary>
    /// <param name="workId">ID of the work</param>
    /// <returns>Enumerable of the work's versions</returns>
    [HttpGet("{workId}/versions")]
    [Produces(typeof(IEnumerable<ViewModels.WorkVersion>))]
    public async Task<IActionResult> GetWorkVersions(string workId)
    {
        var id = Guid.Parse(workId);
        var work = await _dbContext.Works
            .Include(w => w.WorkVersions)
            .FirstOrDefaultAsync(w => w.Id == id);
        if (work == null)
            return NotFound();
        if (work.WorkVersions == null)
            return Ok(new List<ViewModels.WorkVersion>());
        var result = work.WorkVersions.Select(ViewModels.WorkVersion.FromDBEntity);
        return Ok(result);
    }
}
