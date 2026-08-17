using asec.Configuration;
using asec.Models;
using asec.Models.Archive;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/digitalobjects")]
public class DigitalObjectController(AsecDBContext dbContext, [FromKeyedServices("LocalObjectStorage")] IMinioClient minioClient, IOptions<LocalObjectStorageConfiguration> storageConfiguration) : ControllerBase
{
    [HttpGet("")]
    [Produces(typeof(List<ViewModels.DigitalObject>))]
    public async Task<IActionResult> ListDigitalObjects()
    {
        var digitalObjects = await dbContext.DigitalObjects
            .Where(o => o.DigitalObjectType == DigitalObjectType.Modification || o.DigitalObjectType == DigitalObjectType.UnplayableParatext)
            .AsNoTracking()
            .ToListAsync();

        return Ok(digitalObjects.Select(ViewModels.DigitalObject.FromDBEntity));
    }

    /// <summary>
    /// Get the details of the specified digital object.
    /// </summary>
    /// <param name="digitalObjectId">ID of the digital object</param>
    /// <returns>Details of the digital object</returns>
    [HttpGet("{digitalObjectId}")]
    [Produces(typeof(ViewModels.DigitalObject))]
    public async Task<IActionResult> GetDigitalObject(string digitalObjectId)
    {
        var id = Guid.Parse(digitalObjectId);
        var digitalObject = await dbContext.DigitalObjects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
        if (digitalObject == null)
            return NotFound();

        if (digitalObject.DigitalObjectType == DigitalObjectType.PlayableObject || digitalObject.DigitalObjectType == DigitalObjectType.GameArtefact)
            return BadRequest("Cannot get details of a playable object or game artefact.");

        return Ok(ViewModels.DigitalObject.FromDBEntity(digitalObject));
    }

    [HttpGet("{digitalObjectId}/download")]
    public async Task<IActionResult> DownloadDigitalObject(string digitalObjectId)
    {
        var id = Guid.Parse(digitalObjectId);
        var digitalObject = await dbContext.DigitalObjects
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (digitalObject == null)
            return NotFound();

        if (digitalObject.DigitalObjectType == DigitalObjectType.PlayableObject || digitalObject.DigitalObjectType == DigitalObjectType.GameArtefact)
            return BadRequest("Cannot download a playable object or game artefact.");

        var filename = Path.Combine(Path.GetTempPath(), digitalObject.FileName);
        var folderName = digitalObject.DigitalObjectType switch
        {
            DigitalObjectType.Modification => storageConfiguration.Value.ModificationFolder,
            DigitalObjectType.UnplayableParatext => storageConfiguration.Value.ParatextFolder,
            _ => throw new InvalidOperationException("Invalid digital object type for download.")
        };

        var args = new GetObjectArgs()
            .WithFile(filename)
            .WithBucket(storageConfiguration.Value.DigitalObjectBucket)
            .WithObject($"{folderName}/{digitalObject.ObjectId}");

        var minioObject = await minioClient.GetObjectAsync(args);
        var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);

        return File(fileStream, "application/octet-stream", digitalObject.FileName);
    }

    [HttpPost("{digitalObjectId}/link")]
    public async Task<IActionResult> LinkDigitalObject(string digitalObjectId, [FromBody] LinkDigitalObjectCommand linkCommand)
    {
        var id = Guid.Parse(digitalObjectId);
        var paratextId = Guid.Parse(linkCommand.ParatextId);

        var digitalObject = await dbContext.DigitalObjects
            .FirstOrDefaultAsync(p => p.Id == id);
        if (digitalObject == null)
            return NotFound("Digital object not found.");

        if (digitalObject.DigitalObjectType == DigitalObjectType.PlayableObject || digitalObject.DigitalObjectType == DigitalObjectType.GameArtefact)
            return BadRequest("Cannot link a playable object or game artefact.");

        var paratext = await dbContext.Paratexts
            .Include(p => p.DigitalObjects)
            .FirstOrDefaultAsync(p => p.Id == paratextId);
        if (paratext == null)
            return NotFound("Paratext not found.");

        if (paratext.DigitalObjects.Any(d => d.Id == digitalObject.Id))
            return BadRequest("Digital object is already linked to the specified paratext.");

        paratext.DigitalObjects.Add(digitalObject);
        await dbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{digitalObjectId}/unlink")]
    public async Task<IActionResult> UnlinkDigitalObject(string digitalObjectId, [FromBody] LinkDigitalObjectCommand linkCommand)
    {
        var id = Guid.Parse(digitalObjectId);
        var paratextId = Guid.Parse(linkCommand.ParatextId);

        var digitalObject = await dbContext.DigitalObjects
            .FirstOrDefaultAsync(p => p.Id == id);
        if (digitalObject == null)
            return NotFound("Digital object not found.");

        if (digitalObject.DigitalObjectType == DigitalObjectType.PlayableObject || digitalObject.DigitalObjectType == DigitalObjectType.GameArtefact)
            return BadRequest("Cannot unlink a playable object or game artefact.");

        var paratext = await dbContext.Paratexts
            .Include(p => p.DigitalObjects)
            .FirstOrDefaultAsync(p => p.Id == paratextId);
        if (paratext == null)
            return NotFound("Paratext not found.");

        if (!paratext.DigitalObjects.Any(d => d.Id == digitalObject.Id))
            return BadRequest("Digital object is not linked to the specified paratext.");

        paratext.DigitalObjects.Remove(digitalObject);
        await dbContext.SaveChangesAsync();

        return Ok();
    }

    public sealed record LinkDigitalObjectCommand(string ParatextId);
}
