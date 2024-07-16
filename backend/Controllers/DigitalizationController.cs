using asec.Digitalization;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/digitalization")]
public class DigitalizationController : ControllerBase
{
    private ILogger<DigitalizationController> _logger;
    private IToolRepository _tools;
    private IProcessManager _processManager;

    public DigitalizationController(ILogger<DigitalizationController> logger, IToolRepository tools, IProcessManager processManager)
    {
        _logger = logger;
        _tools = tools;
        _processManager = processManager;
    }

    [HttpGet("tools")]
    public IActionResult GetDigitalizationTools()
    {
        var result = _tools.GetDigitalizationTools().Select(tool => new DigitalizationTool (
            tool.Id,
            tool.Name,
            tool.Version,
            tool.PhysicalMedia.ToString(),
            tool.IsAvailable
        ));
        return Ok(result);
    }

    [HttpPut("start")]
    public IActionResult StartDigitalizationProcess([FromBody] DigitalizationRequest request)
    {
        throw new NotImplementedException();
    }

    [HttpPost("{processId}/finalize")]
    public IActionResult FinalizeVersionArtifact(string processId, [FromBody] Artefact artefact)
    {
        throw new NotImplementedException();
    }

    [HttpPost("{processId}/input")]
    public IActionResult ProvideDigitalizationInput(string processId, [FromBody] DigitalizationInput input)
    {
        throw new NotImplementedException();
    }

    [HttpGet("{processId}/log")]
    public IActionResult GetProcessLog(string processId)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        return File(process.LogPath, "text/plain", true);
    }

    [HttpPost("{processId}/restart")]
    public IActionResult RestartDigitalizationProcess(string processId)
    {
        throw new NotImplementedException();
    }

    [HttpGet("{processId}/status")]
    public IActionResult GetDigitalizationProcessStatus(string processId)
    {
        var process = _processManager.GetProcess(Guid.Parse(processId));
        return Ok(DigitalizationProcess.FromProcess(process));
    }


}