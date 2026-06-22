using asec.Models;
using asec.Models.Digitalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;

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
    private readonly IMinioClient _minioClient;
    private readonly string _minioArtefactBucket;

    public ArtefactController(AsecDBContext dbContext, [FromKeyedServices("LocalObjectStorage")] IMinioClient minioClient, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _minioClient = minioClient;
        _minioArtefactBucket = configuration.GetSection("LocalObjectStorage").GetValue<string>("ArtefactBucket");
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
    /// Gets the specified artefact's file contents.
    /// </summary>
    /// <param name="artefactId">ID of the artefact to find</param>
    /// <returns>Contents of the specified artefact if found, 404 otherwise</returns>
    [HttpGet("{artefactId}/download")]
    public async Task<IActionResult> DownloadArtefact(string artefactId)
    {
        var id = Guid.Parse(artefactId);
        var artefact = await _dbContext.DigitalObjects
            .OfType<Artefact>()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (artefact == null)
            return NotFound();

        var filename = Path.Combine(Path.GetTempPath(), artefact.FileName);
        var args = new GetObjectArgs()
            .WithFile(filename)
            .WithBucket(_minioArtefactBucket)
            .WithObject(artefact.ObjectId.ToString());

        var minioObject = await _minioClient.GetObjectAsync(args);
        var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);

        return File(fileStream, "application/octet-stream", artefact.FileName);
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
        if (!String.IsNullOrEmpty(iartefact.Type))
        {
            if (!Enum.TryParse<ArtefactType>(iartefact.Type, out var artefactType))
                return BadRequest();
            if (artefact.PhysicalMediaType == PhysicalMediaType.None)
                artefact.Type = artefactType;
        }
        if (!String.IsNullOrEmpty(iartefact.WebsiteUrl))
        {
            if (!Uri.TryCreate(iartefact.WebsiteUrl, UriKind.Absolute, out var artefactUrl))
                return BadRequest();
            artefact.WebsiteUrl = artefactUrl;
        }
        // TODO: move to viewmodel? also fixup according to CA management
        artefact.Label = iartefact.Label;
        artefact.InternalNote = iartefact.InternalNote;
        artefact.Quality = iartefact.Quality;

        await _dbContext.SaveChangesAsync();

        return Ok(ViewModels.Artefact.FromDBEntity(artefact));
    }
}
