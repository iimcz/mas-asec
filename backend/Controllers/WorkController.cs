using asec.Models;
using asec.Models.Archive;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asec.Controllers;

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


    [HttpGet]
    public async Task<IActionResult> GetWorks()
    {
        var result = (await _dbContext.Works
            .Include(w => w.Genre)
            .Include(w => w.Status)
            .Include(w => w.Classification)
            .Include(w => w.TimeClassification)
            .Include(w => w.LocationClassification)
            .ToListAsync())
            .Select(asec.ViewModels.Work.FromDbEntity);
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> AddWork([FromBody] ViewModels.Work work)
    {
        // Ignore incoming Id, we will generate a new one anyway.
        work.Id = String.Empty;
        var dbWork = await work.ToDbEntity(_dbContext, true);
        dbWork.Id = Guid.NewGuid();

        await _dbContext.Works.AddAsync(dbWork);
        await _dbContext.SaveChangesAsync();
        _logger.LogDebug("New Work created: {0}", dbWork.Id.ToString());

        return Ok(ViewModels.Work.FromDbEntity(dbWork));
    }

    [HttpGet("{workId}")]
    public async Task<IActionResult> GetWork(string workId)
    {
        var id = Guid.Parse(workId);
        var result = await _dbContext.Works
            .Include(w => w.Genre)
            .Include(w => w.Status)
            .Include(w => w.Classification)
            .Include(w => w.TimeClassification)
            .Include(w => w.LocationClassification)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (result == null)
            return NotFound();

        _logger.LogDebug("Found work with id: {0}", workId);
        return Ok(ViewModels.Work.FromDbEntity(result));
    }

    [HttpPost("{workId}")]
    public async Task<IActionResult> UpdateWork(string workId, [FromBody] ViewModels.Work work)
    {
        var id = Guid.Parse(workId);
        if (await _dbContext.Works.FindAsync(id) == null)
            return NotFound();

        _logger.LogDebug("Work found, updating work id: {0}", workId);
        work.Id = workId;
        var dbWork = await work.ToDbEntity(_dbContext, true);

        await _dbContext.SaveChangesAsync();
        return Ok(ViewModels.Work.FromDbEntity(dbWork));
    }

    [HttpGet("{workId}/versions")]
    public async Task<IActionResult> GetWorkVersions(string workId)
    {
        var work = await _dbContext.Works
            .Include(w => w.Versions)
            .ThenInclude(v => v.Status)
            .Include(w => w.Versions)
            .ThenInclude(v => v.System)
            .FirstOrDefaultAsync(w => w.Id == Guid.Parse(workId));
        if (work == null)
            return NotFound();
        if (work.Versions == null)
            return Ok(new List<ViewModels.Version>());
        var result = work.Versions.Select(ViewModels.Version.FromDBEntity);
        return Ok(result);
    }

    [HttpPut("{workId}/paratexts")]
    public async Task<IActionResult> AddWorkParatext(string workId, [FromBody] ViewModels.Paratext paratext)
    {
        var id = Guid.Parse(workId);
        var work = await _dbContext.Works.FindAsync(id);
        if (work == null)
            return NotFound();
        // TODO: somehow choose a proper thumbnail
        var newParatext = new Models.Archive.Paratext() {
            Id = Guid.NewGuid(),
            Work = work,
            Name = paratext.Name,
            Description = paratext.Description,
            Source = paratext.Source,
            SourceUrl = paratext.SourceUrl,
            Thumbnail = "template:unknown",
            Downloadable = false,
        };
        _dbContext.Paratexts.Add(newParatext);
        await _dbContext.SaveChangesAsync();
        return Ok(ViewModels.Paratext.FromDBParatext(newParatext));
    }

    [HttpGet("{workId}/paratexts")]
    public async Task<IActionResult> GetWorkParatexts(string workId)
    {
        var id = Guid.Parse(workId);
        var work = await _dbContext.Works
            .Include(w => w.Paratexts)
            .ThenInclude(p => p.Version)
            .Include(w => w.Paratexts)
            .ThenInclude(p => p.GamePackage)
            .FirstOrDefaultAsync(w => w.Id == id);
        if (work == null)
            return NotFound();
        return Ok(work.Paratexts.Select(p => ViewModels.Paratext.FromDBParatext(p)));
    }
}