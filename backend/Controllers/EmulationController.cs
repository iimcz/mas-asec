using asec.Emulation;
using asec.LongRunning;
using asec.Models;
using asec.Models.Archive;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Minio;
using Minio.DataModel.Args;

namespace asec.Controllers;

/// <summary>
/// Controller dealing with running emulations. Does not actually start an emulation process - that happens
/// in the GamePackage controller as emulation is started from an existing GamePackage.
/// </summary>
[ApiController]
[Route("/api/v1/emulation")]
public class EmulationController : ControllerBase
{
    private readonly string _emulationStreamBaseUrl;
    private readonly string _paratextBucket;
    private readonly IProcessManager<Process, EmulationResult> _processManager;
    private readonly AsecDBContext _dbContext;
    private readonly IMinioClient _minioClient;

    public EmulationController(IProcessManager<Process, EmulationResult> processManager, AsecDBContext dbContext, IMinioClient minioClient, IConfiguration configuration)
    {
        _processManager = processManager;
        _dbContext = dbContext;
        _minioClient = minioClient;
        _emulationStreamBaseUrl = configuration.GetSection("Emulation").GetValue<string>("StreamBaseUrl");
        _paratextBucket = configuration.GetSection("ObjectStorage").GetValue<string>("ParatextBucket");
    }

    /// <summary>
    /// Ping a running emulation to ensure it is not deleted for inactivity. Also returns the current
    /// state of the emulation process.
    /// </summary>
    /// <param name="emulationId">ID of the emulation process</param>
    /// <returns>Details of the emulation process</returns>
    [HttpGet("{emulationId}/ping")]
    public async Task<IActionResult> PingRunningEmulation(string emulationId)
    {
        var id = Guid.Parse(emulationId);
        var process = _processManager.GetProcess(id);
        if (process == null)
            return NotFound();
        await process.ChannelWriter.WriteAsync(Process.EmulationMessage.Ping);
        return Ok(EmulationState.FromProcess(process));
    }

    /// <summary>
    /// End a running emulation, optionally saving recordings.
    /// </summary>
    /// <param name="emulationId">ID of the emulation to finish</param>
    /// <param name="finishRequest">Details on what to save from the finished emulation</param>
    /// <returns>Nothing</returns>
    [HttpPost("{emulationId}/finish")]
    public async Task<IActionResult> FinishEmulation(string emulationId, [FromBody] EmulationFinishRequest finishRequest)
    {
        var id = Guid.Parse(emulationId);
        var process = _processManager.GetProcess(id);
        if (process == null)
            return NotFound();
        await process.ChannelWriter.WriteAsync(
            finishRequest.SaveMachineState ? Process.EmulationMessage.SaveMachineState : Process.EmulationMessage.NoSaveMachineState);
        await process.ChannelWriter.WriteAsync(Process.EmulationMessage.Quit);
        var result = await _processManager.FinishProcessAsync(id);
        
        // TODO: do something with result.SnapshotId...
        Response.OnCompleted(async () => {
            var gamePackage = await _dbContext.GamePackages.Include(p => p.Version).ThenInclude(v => v.Work).FirstAsync(p => p.Id == process.PackageId);
            foreach (var videoFile in result.VideoFiles)
            {
                if (videoFile.Type == RecordingType.Screen && finishRequest.KeepScreenRecording)
                {
                    var filename = Path.GetFileName(videoFile.Path);
                    var dbParatext = new Models.Archive.Paratext() {
                        Id = Guid.NewGuid(),
                        Filename = $"rec-{process.Id}-{filename}",
                        Name = $"Screen of session at {DateTime.UtcNow}",
                        Description = $"Recording of the screen for emulation session ID: {process.Id}",
                        Downloadable = true,
                        GamePackage = gamePackage,
                        Version = gamePackage.Version,
                        Work = gamePackage.Version.Work,
                        Source = "SessionRecording",
                        SourceUrl = null,
                        Thumbnail = "template:video"
                    };
                    var minioArgs = new PutObjectArgs()
                        .WithBucket(_paratextBucket)
                        .WithObject(dbParatext.Id.ToString())
                        .WithFileName(videoFile.Path);
                    // TODO: check for success
                    await _minioClient.PutObjectAsync(minioArgs);
                    _dbContext.Paratexts.Add(dbParatext);
                    await _dbContext.SaveChangesAsync();
                }
                else if (videoFile.Type == RecordingType.Webcam && finishRequest.KeepWebcamRecording)
                {
                    var filename = Path.GetFileName(videoFile.Path);
                    var dbParatext = new Models.Archive.Paratext() {
                        Id = Guid.NewGuid(),
                        Filename = $"rec-{process.Id}-{filename}",
                        Name = $"Webcam of session at {DateTime.UtcNow}",
                        Description = $"Recording of the webcam for emulation session ID: {process.Id}",
                        Downloadable = true,
                        GamePackage = gamePackage,
                        Version = gamePackage.Version,
                        Work = gamePackage.Version.Work,
                        Source = "SessionRecording",
                        SourceUrl = null,
                        Thumbnail = "template:video"
                    };
                    var minioArgs = new PutObjectArgs()
                        .WithBucket(_paratextBucket)
                        .WithObject(dbParatext.Id.ToString())
                        .WithFileName(videoFile.Path);
                    // TODO: check for success
                    await _minioClient.PutObjectAsync(minioArgs);
                    _dbContext.Paratexts.Add(dbParatext);
                    await _dbContext.SaveChangesAsync();
                }
            }
        });

        return Ok();
    }

    /// <summary>
    /// Get an URL that can be used to watch the video stream of a running emulation process.
    /// Useful for including via an iframe.
    /// </summary>
    /// <param name="emulationId">ID of the emulation</param>
    /// <returns>URL of the video stream</returns>
    [HttpGet("{emulationId}/video")]
    public IActionResult GetVideoStreamFrame(string emulationId)
    {
        var id = Guid.Parse(emulationId);
        var emulationProcess = _processManager.GetProcess(id);
        if (emulationProcess == null)
            return NotFound();
        return Ok(_emulationStreamBaseUrl + id.ToString());
    }
}