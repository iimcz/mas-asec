using asec.Emulation;
using asec.LongRunning;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/emulation")]
public class EmulationController : ControllerBase
{
    private IProcessManager<Process, EmulationResult> _processManager;

    public EmulationController(IProcessManager<Process, EmulationResult> processManager)
    {
        _processManager = processManager;
    }

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

        foreach (var videoFile in result.VideoFiles)
        {
            if (videoFile.Type == RecordingType.Screen && finishRequest.KeepScreenRecording)
            {
                // TODO: upload as a paratext
            }
            else if (videoFile.Type == RecordingType.Webcam && finishRequest.KeepWebcamRecording)
            {
                // TODO: upload as a paratext
            }
        }

        return Ok();
    }

    [HttpGet("{emulationId}/video")]
    public IActionResult GetVideoStreamFrame(string emulationId)
    {
        // TODO: separate streams for each emulation
        return Ok("http://adept2:8889/machine/");
    }
}