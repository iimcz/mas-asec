using asec.Models;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/paratexts")]
public class ParatextController : ControllerBase
{
    private readonly string _bucketName;

    private AsecDBContext _dbContext;
    private IMinioClient _minioClient;

    public ParatextController(AsecDBContext dbContext, IMinioClient minioClient, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _minioClient = minioClient;

        _bucketName = configuration.GetSection("ObjectStorage").GetValue<string>("ParatextBucket");
    }
    
    [HttpGet("{paratextId}")]
    public async Task<IActionResult> GetParatext(string paratextId)
    {
        var id = Guid.Parse(paratextId);
        var dbParatext = await _dbContext.Paratexts
            .Include(p => p.Work)
            .Include(p => p.Version)
            .Include(p => p.GamePackage)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (dbParatext == null)
            return NotFound();
        return Ok(Paratext.FromDBParatext(dbParatext));
    }

    [HttpPost("{paratextId}")]
    public async Task<IActionResult> UpdateParatext(string paratextId, [FromBody] Paratext paratext)
    {
        var id = Guid.Parse(paratextId);
        var dbParatext = await _dbContext.Paratexts
            .Include(p => p.Work)
            .Include(p => p.Version)
            .Include(p => p.GamePackage)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (dbParatext == null)
            return NotFound();

        dbParatext.Name = paratext.Name;
        dbParatext.Description = paratext.Description;
        dbParatext.Source = paratext.Source;
        dbParatext.SourceUrl = paratext.SourceUrl;
        await _dbContext.SaveChangesAsync();

        return Ok(Paratext.FromDBParatext(dbParatext));
    }

    [HttpPost("{paratextId}/upload/{filename}")]
    public async Task<IActionResult> UploadParatextFile(string paratextId, string filename, [FromBody] IFormFile file)
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

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithFileName(tmpFile)
            .WithObject(dbParatext.Id.ToString());
        await _minioClient.PutObjectAsync(putObjectArgs);
        dbParatext.Downloadable = true;
        dbParatext.Filename = filename;
        System.IO.File.Delete(tmpFile);
        await _dbContext.SaveChangesAsync();

        return Ok(Paratext.FromDBParatext(dbParatext));
    }

    [HttpGet("{paratextId}/download")]
    public async Task<IActionResult> DownloadParatextFile(string paratextId)
    {
        var id = Guid.Parse(paratextId);
        var dbParatext = await _dbContext.Paratexts.FindAsync(id);
        if (dbParatext == null)
            return NotFound();

        if (!dbParatext.Downloadable)
            return BadRequest();

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
    }
}
