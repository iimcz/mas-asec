using asec.Models;
using asec.Models.Digitalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/artefacts")]
public class ArtefactController : ControllerBase
{
    private readonly AsecDBContext _dbContext;

    public ArtefactController(AsecDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Route("{artefactId}")]
    public async Task<IActionResult> GetArtefact(string artefactId)
    {
        var id = Guid.Parse(artefactId);
        var artefact = await _dbContext.Artefacts
            .Include(a => a.Version)
            .Include(a => a.DigitalizationTool)
            .FirstOrDefaultAsync<Artefact>(a => a.Id == id);
        if (artefact == null)
            return NotFound();
        return Ok(ViewModels.Artefact.FromDBEntity(artefact));
    }
}