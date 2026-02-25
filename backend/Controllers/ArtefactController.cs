using asec.Models;
using asec.Models.Digitalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asec.Controllers;

/// <summary>
/// Allows some interaction with artefacts. Actual creation of these is usually handled by digitalization
/// or another similar process, creating the artefact from existing physical media.
/// </summary>
[ApiController]
[Route("/api/v1/artefacts")]
public class ArtefactController : ControllerBase
{
    private readonly AsecDBContext _dbContext;

    public ArtefactController(AsecDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets the specified artefact's details.
    /// </summary>
    /// <param name="artefactId">ID of the artefact to find</param>
    /// <returns>Details of the specified artefact if found, 404 otherwise</returns>
    [HttpGet("{artefactId}")]
    [Produces(typeof(ViewModels.Artefact))]
    public async Task<IActionResult> GetArtefact(string artefactId)
    {
        var id = Guid.Parse(artefactId);
        var artefact = await _dbContext.DigitalObjects
            .OfType<Models.Digitalization.Artefact>()
            .Include(a => a.WorkVersions)
            .Include(a => a.Paratexts)
            .Include(a => a.DigitalizationTool)
            .FirstOrDefaultAsync<Artefact>(a => a.Id == id);
        if (artefact == null)
            return NotFound();
        return Ok(ViewModels.Artefact.FromDBEntity(artefact));
    }

    /// <summary>
    /// Update the details of the specified artefact.
    /// </summary>
    /// <param name="artefactId">ID of the artefact</param>
    /// <param name="iartefact">Details of the artefact</param>
    /// <returns>The updated artefact</returns>
    [HttpPost("{artefactId}")]
    [Produces(typeof(ViewModels.Artefact))]
    public async Task<IActionResult> UpdateArtefact(string artefactId, [FromBody] ViewModels.Artefact iartefact)
    {
        var id = Guid.Parse(artefactId);
        var artefact = await _dbContext.DigitalObjects
            .OfType<Models.Digitalization.Artefact>()
            .Include(a => a.WorkVersions)
            .Include(a => a.Paratexts)
            .Include(a => a.DigitalizationTool)
            .FirstOrDefaultAsync<Artefact>(a => a.Id == id);
        if (artefact == null)
            return NotFound();
        // TODO: move to viewmodel? also fixup according to CA management
        artefact.InternalNote = iartefact.InternalNote;
        artefact.Quality = iartefact.Quality;
        artefact.Format = iartefact.Format;
        await _dbContext.SaveChangesAsync();

        return Ok(ViewModels.Artefact.FromDBEntity(artefact));
    }
}
