using asec.Compatibility.CollectiveAccess;
using asec.Compatibility.CollectiveAccess.Models;
using asec.Models;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;

namespace asec.Controllers;

/// <summary>
/// Controller providing information about paratexts and allowing their modification.
/// </summary>
[ApiController]
[Route("/api/v1/paratexts")]
public class ParatextController : ControllerBase
{
    private readonly string _bucketName;

    private readonly AsecDBContext _dbContext;
    private readonly IMinioClient _minioClient;
    private readonly SearchClient _caSearchClient;
    private readonly ItemClient _caItemClient;

    public ParatextController(AsecDBContext dbContext, [FromKeyedServices("LocalObjectStorage")] IMinioClient minioClient, SearchClient searchClient, ItemClient itemClient, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _minioClient = minioClient;
        _caSearchClient = searchClient;
        _caItemClient = itemClient;

        _bucketName = configuration.GetSection("LocalObjectStorage").GetValue<string>("ParatextBucket");
    }

    /// <summary>
    /// Rudimentary search for remote paratexts located in the CollectiveAccess database.
    /// </summary>
    /// <param name="searchTerm">What to search for</param>
    /// <returns>List of all the paratexts found in the remote DB</returns>
    [HttpGet("remote")]
    [Produces(typeof(List<RemoteParatext>))]
    public async Task<IActionResult> ListRemoteParatexts(string searchTerm)
    {
        var foundParatexts = await _caSearchClient.GetParatexts(searchTerm);

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
    [HttpGet("remote/{id:int}")]
    [Produces(typeof(ViewModels.Paratext))]
    public async Task<IActionResult> GetRemoteParatext(int id)
    {
        var caParatext = await _caItemClient.GetParatext(id);
        var dbParatext = await _dbContext.Paratexts.Where(p => p.RemoteId == id).FirstOrDefaultAsync();

        if (caParatext == null)
            return NotFound();

        if (dbParatext == null)
        {
            dbParatext = new Models.Archive.Paratext()
            {
                RemoteId = id,
                CanExport = false,
            };
            _dbContext.Paratexts.Add(dbParatext);
        }

        dbParatext.Label = caParatext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceLabel);
        dbParatext.Language = caParatext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceLanguage);
        dbParatext.Date = caParatext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceDate);
        dbParatext.FilledOutBy = caParatext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceFilledOutBy);
        dbParatext.EmissionSize = caParatext.Bundles.GetOptionalBundleUintValue(BundleCodes.OccurrenceEmissionSize) ?? 0;
        dbParatext.IdentificationNumber = caParatext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceIdentificationNumber);
        dbParatext.ParatextType = caParatext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceParatextType);

        await _dbContext.SaveChangesAsync();

        return Ok(ViewModels.Paratext.FromDBEntity(dbParatext));
    }

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
        var dbParatext = await _dbContext.Paratexts
            .Include(p => p.PhysicalObjects)
            .Include(p => p.DigitalObjects)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (dbParatext == null)
            return NotFound();
        return Ok(ViewModels.Paratext.FromDBEntity(dbParatext));
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
        var dbParatext = await _dbContext.Paratexts.FindAsync(id);
        if (dbParatext == null)
            return NotFound();

        dbParatext.Language = paratext.Language;
        dbParatext.Date = paratext.Date;
        dbParatext.InternalNote = paratext.InternalNote;
        dbParatext.FilledOutBy = paratext.FilledOutBy;
        dbParatext.WebsiteUrl = paratext.WebsiteUrl;
        dbParatext.EmissionSize = paratext.EmissionSize;
        dbParatext.IdentificationNumber = paratext.IdentificationNumber;
        dbParatext.ParatextType = paratext.ParatextType;

        await _dbContext.SaveChangesAsync();

        return Ok(ViewModels.Paratext.FromDBEntity(dbParatext));
    }
}
