using asec.Emulation;
using asec.LongRunning;
using asec.Models;
using asec.Models.Recording;
using asec.Platforms;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.Tags;

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
    private readonly IProcessManager<BaseProcess, EmulationResult, EmulationProcessDetail> _processManager;
    private readonly AsecDBContext _dbContext;
    private readonly IMinioClient _minioClient;

    public EmulationController(IProcessManager<BaseProcess, EmulationResult, EmulationProcessDetail> processManager, AsecDBContext dbContext, [FromKeyedServices("LocalObjectStorage")] IMinioClient minioClient, IConfiguration configuration)
    {
        _processManager = processManager;
        _dbContext = dbContext;
        _minioClient = minioClient;
        _emulationStreamBaseUrl = configuration.GetSection("Emulation").GetValue<string>("StreamBaseUrl");
        _paratextBucket = configuration.GetSection("LocalObjectStorage").GetValue<string>("ParatextBucket");
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
        await process.ChannelWriter.WriteAsync(BaseProcess.EmulationMessage.Ping);
        return Ok(EmulationProcess.FromProcess(process));
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
        var process = _processManager.GetProcess(id) as KioskProcess;
        if (process == null)
            return NotFound();
        await process.ChannelWriter.WriteAsync(
            finishRequest.SaveMachineState ? BaseProcess.EmulationMessage.SaveMachineState : BaseProcess.EmulationMessage.NoSaveMachineState);
        await process.ChannelWriter.WriteAsync(BaseProcess.EmulationMessage.Quit);
        var result = await _processManager.FinishProcessAsync(id);

        var package = await _dbContext.DigitalObjects.OfType<Models.Emulation.PlayableObject>().Include(p => p.WorkVersions).FirstOrDefaultAsync(p => p.Id == process.PackageId);
        if (package == null)
            return NotFound();
        var version = package.WorkVersions?.FirstOrDefault();

        foreach (var videoFile in result.VideoFiles)
        {
            if ((videoFile.Type == RecordingType.Screen && finishRequest.KeepScreenRecording) || (videoFile.Type == RecordingType.Webcam && finishRequest.KeepWebcamRecording))
            {
                var paratext = new Models.Archive.Paratext()
                {
                    Id = Guid.NewGuid(),
                    ParatextType = "Video recording",
                    Label = $"{package.Label} gameplay",
                    InternalNote = $"{videoFile.Type} recording for emulationId: {emulationId}",
                    CanExport = true,
                    Date = DateTime.Now.ToString(),
                };

                var fileInfo = new FileInfo(videoFile.Path);

                var videoRecording = new VideoRecording()
                {
                    Id = Guid.NewGuid(),
                    FileName = $"{emulationId}-{fileInfo.Name}",
                    Format = "mp4",
                    FileSize = fileInfo.Length,
                    DigitalObjectType = Models.Archive.DigitalObjectType.UnplayableParatext,
                    MediaInfoReport = await Linux.MediaInfo(["--Output=JSON", videoFile.Path]),
                    Label = $"Video of {package.Label}",
                    RecordingType = videoFile.Type,
                    Paratexts = [paratext]
                };

                var tags = new Dictionary<string, string>()
                {
                    { "Tag", "Paratext" },
                    { "DataType", "MP4 File" }
                };
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_paratextBucket)
                    .WithFileName(videoFile.Path)
                    .WithTagging(new Tagging(tags, true))
                    .WithObject(paratext.Id.ToString());
                await _minioClient.PutObjectAsync(putObjectArgs);

                _dbContext.Paratexts.Add(paratext);
                _dbContext.DigitalObjects.Add(videoRecording);
                await _dbContext.SaveChangesAsync();
            }
        }

        // TODO: do something with result.SnapshotId...

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
