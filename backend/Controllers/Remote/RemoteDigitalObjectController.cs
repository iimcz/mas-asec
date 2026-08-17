using Microsoft.AspNetCore.Mvc;

namespace asec.Controllers.Remote;

[ApiController]
[Route("/api/v1/remotedigitalobjects")]
public class RemoteDigitalObjectController : ControllerBase
{
    [HttpGet("")]
    [Produces(typeof(List<ViewModels.RemoteDigitalObject>))]
    public async Task<IActionResult> ListRemoteDigitalObjects(string searchTerm)
    {
        throw new NotImplementedException();
    }

    [HttpGet("{id:int}")]
    [Produces(typeof(ViewModels.RemoteDigitalObject))]
    public async Task<IActionResult> GetRemoteDigitalObjects(int id)
    {
        throw new NotImplementedException();
    }

    [HttpPost("{digitalObjectId:int}/link")]
    public async Task<IActionResult> LinkRemoteDigitalObject(int digitalObjectId, [FromBody] LinkRemoteDigitalObjectCommand linkCommand)
    {
        throw new NotImplementedException();
    }

    [HttpPost("{digitalObjectId:int}/unlink")]
    public async Task<IActionResult> UnlinkRemoteDigitalObject(int digitalObjectId, [FromBody] LinkRemoteDigitalObjectCommand linkCommand)
    {
        throw new NotImplementedException();
    }

    public sealed record LinkRemoteDigitalObjectCommand(int RemoteParatextId);
}
