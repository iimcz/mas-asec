using asec.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asec.Controllers;

/// <summary>
/// Controller providing information about paratexts and allowing their modification.
/// </summary>
[ApiController]
[Route("/api/v1/paratexts")]
public class ParatextController(AsecDBContext dbContext) : ControllerBase
{
    /// <summary>
    /// Get the details of the specified paratext.
    /// </summary>
    /// <param name="paratextId">ID of the paratext</param>
    /// <returns>Details of the paratext</returns>
    [HttpGet("{paratextId}")]
    [Produces(typeof(ViewModels.Paratext))]
    public async Task<IActionResult> GetParatext(string paratextId)
    {
        var id = Guid.Parse(paratextId);
        var dbParatext = await dbContext.Paratexts
            .Include(p => p.PhysicalObjects)
            .Include(p => p.DigitalObjects)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (dbParatext == null)
            return NotFound();
        return Ok(ViewModels.Paratext.FromDBEntity(dbParatext));
    }

    [HttpGet("{paratextId}/digitalobjects")]
    [Produces(typeof(List<ViewModels.DigitalObject>))]
    public async Task<IActionResult> GetDigitalObjects(string paratextId)
    {
        var id = Guid.Parse(paratextId);
        var dbParatext = await dbContext.Paratexts
            .AsNoTracking()
            .Include(p => p.DigitalObjects)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (dbParatext == null)
            return NotFound();
        return Ok(dbParatext.DigitalObjects.Select(ViewModels.DigitalObject.FromDBEntity));
    }

    /// <summary>
    /// Update the details of the specified paratext.
    /// </summary>
    /// <param name="paratextId">ID of the paratext to update</param>
    /// <param name="paratext">New details of the paratext</param>
    /// <returns>The updated paratext</returns>
    [HttpPost("{paratextId}")]
    [Produces(typeof(ViewModels.Paratext))]
    public async Task<IActionResult> UpdateParatext(string paratextId, [FromBody] ViewModels.Paratext paratext)
    {
        var id = Guid.Parse(paratextId);

        var dbParatext = await dbContext.Paratexts.FindAsync(id);
        if (dbParatext == null)
            return NotFound();

        if (!dbParatext.CanExport)
        {
            return BadRequest("Cannot update a paratext that cannot be exported");
        }

        dbParatext.Label = paratext.Label;
        dbParatext.Language = paratext.Language;
        dbParatext.Date = paratext.Date;
        dbParatext.InternalNote = paratext.InternalNote;
        dbParatext.FilledOutBy = paratext.FilledOutBy;
        dbParatext.WebsiteUrl = paratext.WebsiteUrl;
        dbParatext.EmissionSize = paratext.EmissionSize;
        dbParatext.IdentificationNumber = paratext.IdentificationNumber;
        dbParatext.ParatextType = paratext.ParatextType;

        await dbContext.SaveChangesAsync();

        return Ok(ViewModels.Paratext.FromDBEntity(dbParatext));
    }
}
