using asec.Compatibility.CollectiveAccess;
using asec.Compatibility.CollectiveAccess.Models;
using asec.Models;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asec.Controllers.Remote;

/// <summary>
/// Controller providing information about remote paratexts and allowing their linking.
/// </summary>
[ApiController]
[Route("/api/v1/remoteparatexts")]
public class RemoteParatextController(AsecDBContext dbContext, SearchClient searchClient, ItemClient itemClient) : ControllerBase
{
    /// <summary>
    /// Rudimentary search for remote paratexts located in the CollectiveAccess database.
    /// </summary>
    /// <param name="searchTerm">What to search for</param>
    /// <returns>List of all the paratexts found in the remote DB</returns>
    [HttpGet("")]
    [Produces(typeof(List<RemoteParatext>))]
    public async Task<IActionResult> ListRemoteParatexts(string searchTerm)
    {
        var foundParatexts = await searchClient.GetParatexts(searchTerm);

        var result = foundParatexts.Select(p => new RemoteParatext()
        {
            Id = p.Id,
            Idno = p.Idno,
            Label = p.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceLabel),
            Note = p.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceInternalNote)
        });
        return Ok(result);
    }

    /// <summary>
    /// Use a remote paratext ID to transparently import the paratext and
    /// return the resulting record. This can be used to manipulate the paratext further,
    /// like to add a digital object to it.
    /// </summary>
    /// <param name="id">Remote ID to import/show</param>
    /// <returns>Imported paratext</returns>
    [HttpGet("{id:int}")]
    [Produces(typeof(ViewModels.Paratext))]
    public async Task<IActionResult> GetRemoteParatext(int id)
    {
        var caParatext = await itemClient.GetParatext(id);
        var dbParatext = await dbContext.Paratexts.Where(p => p.RemoteId == id).FirstOrDefaultAsync();

        if (caParatext == null)
            return NotFound();

        if (dbParatext == null)
        {
            dbParatext = new Models.Archive.Paratext()
            {
                RemoteId = id,
                CanExport = false,
            };
            dbContext.Paratexts.Add(dbParatext);
        }

        dbParatext.Label = caParatext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceLabel);
        dbParatext.Language = caParatext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceLanguage);
        dbParatext.Date = caParatext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceDate);
        dbParatext.FilledOutBy = caParatext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceFilledOutBy);
        dbParatext.EmissionSize = caParatext.Bundles.GetOptionalBundleUintValue(BundleCodes.OccurrenceEmissionSize) ?? 0;
        dbParatext.IdentificationNumber = caParatext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceIdentificationNumber);
        dbParatext.ParatextType = caParatext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceParatextType);

        await dbContext.SaveChangesAsync();

        return Ok(ViewModels.Paratext.FromDBEntity(dbParatext));
    }
}