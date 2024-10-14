using asec.Models;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;
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
        var dbParatext = await _dbContext.Paratexts.FindAsync(id);
        if (dbParatext == null)
            return NotFound();
        return Ok(Paratext.FromDBParatext(dbParatext));
    }

    [HttpPost("{paratextId}")]
    public async Task<IActionResult> UpdateParatext(string paratextId, [FromBody] Paratext paratext)
    {
        var id = Guid.Parse(paratextId);
        var dbParatext = await _dbContext.Paratexts.FindAsync(id);
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
    public async Task<IActionResult> UploadParatextFile(string paratextId, string filename)
    {
        var id = Guid.Parse(paratextId);
        var dbParatext = await _dbContext.Paratexts.FindAsync(id);
        if (dbParatext == null)
            return NotFound();

        var stream = Request.Body;
        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithStreamData(stream)
            .WithObject(dbParatext.Id.ToString());
        await _minioClient.PutObjectAsync(putObjectArgs);
        dbParatext.Downloadable = true;
        dbParatext.Filename = filename;
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
