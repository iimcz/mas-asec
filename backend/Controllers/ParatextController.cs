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

    public ParatextController(AsecDBContext dbContext, [FromKeyedServices("LocalObjectStorage")] IMinioClient minioClient, SearchClient searchClient, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _minioClient = minioClient;
        _caSearchClient = searchClient;

        _bucketName = configuration.GetSection("LocalObjectStorage").GetValue<string>("ParatextBucket");
    }

    /// <summary>
    /// Rudimentary search for remote paratexts located in the CollectiveAccess database.
    /// </summary>
    /// <param name="searchTerm">What to search for</param>
    /// <returns>List of all the paratexts found in the remote DB</returns>
    [HttpGet("all")]
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
            .Include(p => p.PhysicalObject)
            .Include(p => p.DigitalObject)
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
