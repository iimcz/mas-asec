using asec.Emulation;
using asec.Models;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.Tags;

namespace asec.Controllers;

/// <summary>
/// Controller providing information about paratexts and allowing their modification.
/// </summary>
[ApiController]
[Route("/api/v1/paratexts")]
public class ParatextController : ControllerBase
{
    private readonly string _bucketName;

    private AsecDBContext _dbContext;
    private IMinioClient _minioClient;

    public ParatextController(AsecDBContext dbContext, [FromKeyedServices("LocalObjectStorage")] IMinioClient minioClient, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _minioClient = minioClient;

        _bucketName = configuration.GetSection("ObjectStorage").GetValue<string>("ParatextBucket");
    }
    
    /// <summary>
    /// Get the details of the specified paratext.
    /// </summary>
    /// <param name="paratextId">ID of the paratext</param>
    /// <returns>Details of the paratext</returns>
    [HttpGet("{paratextId}")]
    [Produces(typeof(Paratext))]
    public async Task<IActionResult> GetParatext(string paratextId)
    {
        var id = Guid.Parse(paratextId);
        var dbParatext = await _dbContext.Paratexts
            .Include(p => p.PhysicalObject)
            .Include(p => p.DigitalObject)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (dbParatext == null)
            return NotFound();
        return Ok(Paratext.FromDBEntity(dbParatext));
    }

    /// <summary>
    /// Update the details of the specified paratext.
    /// </summary>
    /// <param name="paratextId">ID of the paratext to update</param>
    /// <param name="paratext">New details of the paratext</param>
    /// <returns>The updated paratext</returns>
    [HttpPost("{paratextId}")]
    [Produces(typeof(Paratext))]
    public async Task<IActionResult> UpdateParatext(string paratextId, [FromBody] Paratext paratext)
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

        return Ok(Paratext.FromDBEntity(dbParatext));
    }

    /// <summary>
    /// Add a file to the paratext. This file will then be available for download.
    /// </summary>
    /// <param name="paratextId">ID of the paratext</param>
    /// <param name="filename">Name of the uploaded file</param>
    /// <param name="file">The uploaded file</param>
    /// <returns>The updated (now downloadable) paratext</returns>
    [HttpPost("{paratextId}/upload/{filename}")]
    [Produces(typeof(Paratext))]
    public async Task<IActionResult> UploadParatextFile(string paratextId, string filename, [FromForm] IFormFile file)
    {
        var id = Guid.Parse(paratextId);
        var dbParatext = await _dbContext.Paratexts.FindAsync(id);
        if (dbParatext == null)
            return NotFound();

        var tmpFile = Path.GetTempFileName();
        using (var fileStream = new FileStream(tmpFile, FileMode.OpenOrCreate, FileAccess.Write))
        {
            await file.CopyToAsync(fileStream);
        }

        var fileInfo = new FileInfo(tmpFile);

        var digitalObject = new Models.Archive.DigitalObject()
        {
            Id = Guid.NewGuid(),
            FileName = filename,
            Format = fileInfo.Extension,
            FileSize = (uint)fileInfo.Length,
            Paratexts = [dbParatext]
        };

        dbParatext.DigitalObject = digitalObject;

        var tags = new Dictionary<string, string>()
        {
            { "Tag", "Paratext" },
            { "DataType", "File" }
        };
        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithFileName(tmpFile)
            .WithTagging(new Tagging(tags, true))
            .WithObject(dbParatext.Id.ToString());
        await _minioClient.PutObjectAsync(putObjectArgs);
        //dbParatext.Downloadable = true;
        //dbParatext.Filename = filename;
        System.IO.File.Delete(tmpFile);

        _dbContext.DigitalObjects.Add(digitalObject);
        await _dbContext.SaveChangesAsync();

        return Ok(Paratext.FromDBEntity(dbParatext));
    }

    /// <summary>
    /// Download the file of the specified paratext.
    /// </summary>
    /// <param name="paratextId">ID of the paratext to download the file of</param>
    /// <returns>Stream of the paratext's file as application/octet-stream</returns>
    [HttpGet("{paratextId}/download")]
    public async Task<IActionResult> DownloadParatextFile(string paratextId)
    {
        var id = Guid.Parse(paratextId);
        var dbParatext = await _dbContext.Paratexts.FindAsync(id);
        if (dbParatext == null)
            return NotFound();

        //if (!dbParatext.Downloadable)
        return BadRequest(); // TODO: currently always a bad request, support pending due to database model changes.

        /*
        // TODO: use some kind of proxy for direct access instead of
        // caching the file ourselves.
        var tmpFile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        var getObjectArgs = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(dbParatext.Id.ToString())
            .WithFile(tmpFile);
        Response.OnCompleted(() =>
        {
            if (Path.Exists(tmpFile))
                System.IO.File.Delete(tmpFile);
            return Task.CompletedTask;
        });
        await _minioClient.GetObjectAsync(getObjectArgs);
        return PhysicalFile(tmpFile, "application/octet-stream", dbParatext.Filename, true);
        */
    }
}
